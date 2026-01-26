
//**********************************************
// Filter generic Interface
// Structured to call Filter required by Name
// All filters have a "Strategy" configuration call
// All filters use ProcessBlock(float[] samples); and return a scalar level result
//**********************************************
using static HilbertStrategy;

public interface ICWFilter
{
    string Name { get; }
    double ProcessBlock(float[] samples);

    // Common properties for UI binding
    double TargetFrequency { get; set; }
    double filterBW { get; set; }
    double Tolerance { get; set; }
    double ScalingFactor { get; set; }

    // New method for logging internal states
    double SigConfidence();
    bool isHilbert();
}

//Common base class for filters
public abstract class MorseFilterBase : ICWFilter
{
    public abstract string Name { get; }
    protected int _sampleRate = 48000;

    // Backing fields
    protected double _tone = 1000;
    protected double _bw = 75;
    protected double _tol = 30;
    protected double _scaling = 200;

    public virtual double TargetFrequency
    {
        get => _tone;
        set { _tone = value; OnSettingsChanged(); }
    }
    public virtual double filterBW
    {
        get => _bw;
        set { _bw = value; OnSettingsChanged(); }
    }
    public virtual double Tolerance
    {
        get => _tol;
        set { _tol = value; } // Usually doesn't require a filter rebuild
    }
    public virtual double ScalingFactor
    {
        get => _scaling;
        set => _scaling = value;
    }

    public virtual double SigConfidence() => 1;         //default return. Hilbert overrides this
    public virtual bool isHilbert() => false;           //default return. Hilbert overrides this

    // Handled by individual filters
    public abstract double ProcessBlock(float[] samples);

    // This method is called whenever Freq or BW changes
    protected virtual void OnSettingsChanged() { }
}


//***************************************************
//Goertzel
//***************************************************
public class GoertzelStrategy : MorseFilterBase
{
    private GoertzelFilter _filter;
    public override string Name => "Goertzel";

    // Constructor ensures the filter is built upon selection
    public GoertzelStrategy(int sampleRate, double targetFreq, int blockSize)
    {
        _sampleRate = sampleRate;
        _tone = targetFreq;
        // Note: For Goertzel, 'blockSize' determines the BW. 
        // We set _bw here just to keep the base class in sync.
        _bw = (double)sampleRate / blockSize;
        OnSettingsChanged();
    }

    // Triggered automatically by MorseFilterBase when _tone or _bw is changed
    protected override void OnSettingsChanged()
    {
        // Calculate block size based on bandwidth if you want dynamic BW,
        // otherwise use a fixed block size (e.g., 256)
        int blockSize = (int)((double)_sampleRate / _bw);
        // Ensure blockSize is at least reasonable
        if (blockSize < 64) blockSize = 64;

        _filter = new GoertzelFilter(_tone, _sampleRate, blockSize);
    }

    public override double ProcessBlock(float[] samples)
    {
        // Goertzel returns power; we scale it to match the expected UI range.
        // The 77 is your original empirical scaling constant.
        return _filter.CalculateMagnitude(samples) * 77 / _scaling;
    }
}

public class GoertzelFilter
{
    private double _coeff;
    private double _targetFreq;
    private int _sampleRate;
    private int _blockSize;

    private readonly float[] _hannWindow;

    public GoertzelFilter(double targetFreq, int sampleRate, int blockSize)
    {
        _targetFreq = targetFreq;
        _sampleRate = sampleRate;
        _blockSize = blockSize;

        // Calculate the coefficient
        double k = (int)(0.5 + ((_blockSize * _targetFreq) / (double) _sampleRate));
        double omega = (2.0 * Math.PI * k) / _blockSize;
        _coeff = 2.0 * Math.Cos(omega);

        _hannWindow = new float[blockSize];
        for (int i = 0; i < blockSize; i++)
        {
            // Hann Window Formula: 0.5 * (1 - cos(2*PI*i / (N-1)))
            _hannWindow[i] = (float)(0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (blockSize - 1))));
        }
    }

    public double CalculateMagnitude(float[] samples)
    {
        double q0 = 0, q1 = 0, q2 = 0, power = 0;

        // Apply Hann windowing DURING the iteration
        for (int i = 0; i < samples.Length && i < _blockSize; i++)
        {
            // Taper the sample before adding to the filter state
            float windowedSample = samples[i] * _hannWindow[i];

            q0 = (_coeff * q1) - q2 + windowedSample;
            q2 = q1;
            q1 = q0;
        }

        power = (q1 * q1 + q2 * q2 - q1 * q2 * _coeff);
        //double magnitude = Math.Sqrt(Math.Max(0, power))       // linear mag
        //return magnitude;

        // Return the squared magnitude (faster than taking Square Root)
        return power;
    }
}

//***********************************************
// FIR
//***********************************************
public class FIRStrategy : MorseFilterBase
{
    private FIRFilter _filter;

    private HilbertFIR _hilbert;
    private HilbertCoherentDetector _coherentDetector;
    private double _lastBlockConfidence = 0;
    public override double SigConfidence() => _lastBlockConfidence;

    private double[] _delayedSig = { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };        // For simple 2-output delay to accomodtae Hilbert delay line
    private int _delayIndex = 0;

    public override string Name => "FIR Bandpass";

    public FIRStrategy(int sampleRate, double freq, double bw)
    {
        _sampleRate = sampleRate;
        _tone = freq;
        _bw = bw;
        OnSettingsChanged();
    }

    protected override void OnSettingsChanged()
    {
        _filter = new FIRFilter(_sampleRate, _tone, _bw, 511);
        _hilbert = new HilbertFIR(511);         //note 511 min for sufficient freq resolution at 48k Fs
        _coherentDetector = new HilbertCoherentDetector(_tone, _sampleRate, _tol);
    }

    public override double ProcessBlock(float[] samples)
    {
        double sumAbs = 0;
        double mag = 0;
        double sumConfidence = 0;         //from Hilbert processing

        //For Hilbert processing - not used in this basic FIR
        double targetDelta = (2.0 * Math.PI * _tone) / _sampleRate;
        double radTol = (2.0 * Math.PI * _tol) / _sampleRate;

        for (int i = 0; i < samples.Length; i++)
        {
            //FIR bandpass filtering
            float filteredSample = _filter.Process(samples[i]);
            sumAbs += Math.Abs(filteredSample);

            var (I, Q) = _hilbert.GetIQ(filteredSample);
            var result = _coherentDetector.Update(I, Q);

            sumConfidence += result.Confidence;
        }
        mag = sumAbs / samples.Length * 50000 / _scaling;
        double conf = sumConfidence / samples.Length;      //this is picked up by decoder
                                                                    //via SigConfidence() call

        //delay line to match Hilbert filter delay 256 sample delay required for 511 tap Hilbert
        //assumes 128 sample blocks, so only 2 block delay needed
        //NOTE can not use overlap with hilbert and this simple delay line
        _delayedSig[0] = _delayedSig[1];    //shift down
        _delayedSig[1] = _delayedSig[2];
        _delayedSig[2] = conf;               //put new result in newest slot

        _lastBlockConfidence = _delayedSig[1];

        return mag;          // now matches Hilbert confidence output
    }
}

public class FIRFilter
{
    private readonly float[] _taps;
    private readonly float[] _buffer;
    private int _bufferIndex = 0;

    public FIRFilter(int sampleRate, double centerFreq, double bandwidth, int tapCount = 127)
    {
        _taps = DesignBandPass(sampleRate, centerFreq, bandwidth, tapCount);
        _buffer = new float[tapCount];
    }

    private float[] DesignBandPass(int rate, double center, double bw, int n)
    {
        float[] taps = new float[n];
        double fLow = (center - bw / 2.0) / rate;
        double fHigh = (center + bw / 2.0) / rate;

        for (int i = 0; i < n; i++)
        {
            if (i == n / 2) // Handle division by zero at center
            {
                taps[i] = (float)(2.0 * (fHigh - fLow));
            }
            else
            {
                double mid = i - n / 2.0;
                taps[i] = (float)((Math.Sin(2 * Math.PI * fHigh * mid) - Math.Sin(2 * Math.PI * fLow * mid)) / (Math.PI * mid));
            }
            // Apply Hamming window to reduce ripples
            //taps[i] *= (float)(0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (n - 1)));

            // --- BLACKMAN WINDOW ---
            // Provides much better stop-band rejection than Hamming
            double alpha = 0.16; // Standard Blackman coefficient
            double a0 = (1 - alpha) / 2.0;
            double a1 = 0.5;
            double a2 = alpha / 2.0;

            double window = a0
                            - a1 * Math.Cos((2 * Math.PI * i) / (n - 1))
                            + a2 * Math.Cos((4 * Math.PI * i) / (n - 1));

            taps[i] *= (float)window;
        }
        return taps;
    }

    //filter an entire block
    public float[] Apply(float[] samples)
    {
        float[] output = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            output[i] = Process(samples[i]);
        }
        return output;
    }

    //convolution of the block for each incoming sample
    public float Process(float sample)
    {
        _buffer[_bufferIndex] = sample;
        float output = 0;
        int index = _bufferIndex;

        for (int i = 0; i < _taps.Length; i++)
        {
            output += _taps[i] * _buffer[index--];
            if (index < 0) index = _taps.Length - 1;
        }

        if (++_bufferIndex >= _taps.Length) _bufferIndex = 0;
        return output;
    }
}


// ****************************************
// RMS
//******************************************
public class RMSStrategy : MorseFilterBase
{
    private RMSFilter _filter;
    public override string Name => "RMS";

    public RMSStrategy(int sampleRate, int size)
    {
        _sampleRate = sampleRate;
        // Use the initial size to set our internal BW
        _bw = (double)sampleRate / size;
        OnSettingsChanged();
    }

    protected override void OnSettingsChanged()
    {
        // Calculate block size based on the BW slider
        // Higher BW = smaller block (faster response)
        // Lower BW = larger block (more smoothing)
        int blockSize = (int)((double)_sampleRate / _bw);

        // Safety bounds for block size
        if (blockSize < 32) blockSize = 32;
        if (blockSize > 4096) blockSize = 4096;

        _filter = new RMSFilter(_sampleRate, blockSize);
    }

    public override double ProcessBlock(float[] samples)
    {
        // 18.7 is your existing empirical normalization constant
        return _filter.CalculateMagnitude(samples) / 18.7 / _scaling;
    }
}

public class RMSFilter
{
    private double _coeff;
    private double _targetFreq;
    private int _sampleRate;
    private int _blockSize;

    public double InputScaleFactor { get; set; } = 1000;
    private readonly float[] _hannWindow;

    public RMSFilter(int sampleRate, int blockSize)
    {
        _sampleRate = sampleRate;
        _blockSize = blockSize;

        // Calculate the coefficient
        double k = (int)(0.5 + ((_blockSize * _targetFreq) / (double)_sampleRate));
        double omega = (2.0 * Math.PI * k) / _blockSize;
        _coeff = 2.0 * Math.Cos(omega);

        _hannWindow = new float[blockSize];
        for (int i = 0; i < blockSize; i++)
        {
            // Hann Window Formula: 0.5 * (1 - cos(2*PI*i / (N-1)))
            _hannWindow[i] = (float)(0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (blockSize - 1))));
        }
    }

    public double CalculateMagnitude(float[] samples)
    {
        double power = 0;

        for (int i = 0; i < samples.Length && i < _blockSize; i++)
        {
            float windowedSample = samples[i] * _hannWindow[i];
            power = power + (windowedSample * windowedSample);
        }
        power = power * 100000;
    
        // Return the squared magnitude (faster than taking Square Root)
        return power;
    }
}


//***********************************************
// Hilbert Transform FIR
//***********************************************
public class HilbertStrategy : MorseFilterBase
{
    private FIRFilter _preFilter;
    private HilbertFIR _hilbert;
    private HilbertCoherentDetector _coherentDetector;

    private double _lastPhase = 0;
    public override string Name => "Hilbert Analytic";

    private double _lastBlockConfidence = 0;
    public override double SigConfidence() => _lastBlockConfidence;
    public override bool isHilbert() => true;   

    public HilbertStrategy(int sampleRate, double freq, double bw)
    {
        _sampleRate = sampleRate;
        _tone = freq;
        _bw = bw;
        OnSettingsChanged();
    }

    protected override void OnSettingsChanged()
    {
        _hilbert = new HilbertFIR(511);         //note 511 min for sufficient freq resolution at 48k Fs
        _preFilter = new FIRFilter(_sampleRate, _tone, _bw, 511);
        _coherentDetector = new HilbertCoherentDetector(_tone, _sampleRate, _tol);
    }

    public override double ProcessBlock(float[] samples)
    {
        double sumConfidence = 0;
        double sumMag = 0;
        float[] cleanSamples = _preFilter.Apply(samples);

        // Core math for 48kHz
        double targetDelta = (2.0 * Math.PI * _tone) / _sampleRate;
        double radTol = (2.0 * Math.PI * _tol) / _sampleRate;

        // This is for the 50% overlap continuity
        int stepSize = cleanSamples.Length / 2;
        double phaseForNextBlock = 0;

        for (int i = 0; i < cleanSamples.Length; i++)
        {
            var (I, Q) = _hilbert.GetIQ(cleanSamples[i]);   //compute in/out phase sample result

            var result = _coherentDetector.Update(I, Q);    //compute coherent detection result

            sumConfidence += result.Confidence;
            sumMag += result.Magnitude;
        }

        _lastBlockConfidence = sumConfidence / cleanSamples.Length;

        // Crucial: Hand off the phase for the next block's first delta calculation
        // If you are NOT using overlap, just leave _lastPhase as is.
        // _lastPhase = phaseForNextBlock; 

        // Return weighted magnitude
        return (sumMag / cleanSamples.Length) * 50000 / _scaling;
    }

    public class HilbertFIR
    {
        private readonly float[] _taps;
        private readonly float[] _buffer;
        private int _bufferIndex = 0;
        private readonly int _mid;

        public HilbertFIR(int taps = 511)
        {
            _taps = new float[taps];
            _buffer = new float[taps];
            _mid = (taps - 1) / 2;

            for (int i = 0; i < taps; i++)
            {
                int n = i - _mid;
                if (n == 0 || n % 2 == 0) _taps[i] = 0;
                else _taps[i] = (float)(2.0 / (Math.PI * n));

                // Hamming window for 48kHz precision
                double window = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (taps - 1));
                _taps[i] *= (float)window;
            }
        }

        public (float I, float Q) GetIQ(float sample)
        {
            _buffer[_bufferIndex] = sample;
            float Q = 0;
            for (int i = 0; i < _taps.Length; i++)
            {
                int idx = (_bufferIndex - i + _taps.Length) % _taps.Length;
                Q += _taps[i] * _buffer[idx];
            }
            int iIdx = (_bufferIndex - _mid + _taps.Length) % _taps.Length;
            float I = _buffer[iIdx];
            _bufferIndex = (_bufferIndex + 1) % _taps.Length;
            return (I, Q);
        }       
    }

    public struct CoherenceResult
    {
        public double Magnitude;
        public double Confidence;
    }

    public class HilbertCoherentDetector
    {
        private double _lastPhase;
        private readonly double _targetDelta;
        private readonly double _radTol;

        public HilbertCoherentDetector(double targetFreq, double sampleRate, double toleranceHz)
        {
            _targetDelta = (2.0 * Math.PI * targetFreq) / sampleRate;
            _radTol = (2.0 * Math.PI * toleranceHz) / sampleRate;
        }

        // This is your "Inner Sample Code" extracted
        public CoherenceResult Update(double I, double Q)
        {
            double magnitude = Math.Sqrt(I * I + Q * Q);
            double currentPhase = Math.Atan2(Q, I);

            // Phase Velocity (Frequency) calculation
            double deltaPhase = currentPhase - _lastPhase;
            while (deltaPhase <= -Math.PI) deltaPhase += 2 * Math.PI;
            while (deltaPhase > Math.PI) deltaPhase -= 2 * Math.PI;

            _lastPhase = currentPhase;

            // Calculate Coherence (Confidence)
            double phaseError = Math.Abs(deltaPhase - _targetDelta);
            double confidence = 0;

            if (phaseError < _radTol)
                confidence = 1.0;
            else if (phaseError < _radTol * 2.5)
                confidence = 1.0 - ((phaseError - _radTol) / (_radTol * 1.5));

            return new CoherenceResult
            {
                Magnitude = magnitude * Math.Pow(confidence, 3), // Cubic weighting
                Confidence = confidence
            };
        }
    }
}
