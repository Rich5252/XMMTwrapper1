
//**********************************************
// Filter generic Interface
// Structured to call Filter required by Name
// All filters have a "Strategy" configuration call
// All filters use ProcessBlock(float[] samples); and return a scalar level result
//**********************************************
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
    double GetDiagnosticValue();
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

    public virtual double GetDiagnosticValue() => 0;        //dummy return. Hilbert overrides this

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
    }

    public override double ProcessBlock(float[] samples)
    {
        double sumAbs = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sumAbs += Math.Abs(_filter.Process(samples[i]));
        }
        return sumAbs / samples.Length * 50000 / _scaling;
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
    private double _lastPhase = 0;
    public override string Name => "Hilbert Analytic";

    private double _lastBlockConfidence = 0;
    public override double GetDiagnosticValue() => _lastBlockConfidence;

    public HilbertStrategy(int sampleRate, double freq, double bw)
    {
        _sampleRate = sampleRate;
        _tone = freq;
        _bw = bw;
        OnSettingsChanged();
    }

    protected override void OnSettingsChanged()
    {
        _hilbert = new HilbertFIR(127);
        _preFilter = new FIRFilter(_sampleRate, _tone, _bw, 511);
    }

    private float _decimationSum = 0;
    private int _decimationCount = 0;
    private const int DecimationFactor = 10;

    public override double ProcessBlock(float[] samples)
    {
        double sumConfidence = 0;
        double sumMag = 0;
        int decimatedProcessedCount = 0;

        // The effective sample rate is now 4800 Hz
        double fsLow = (double)_sampleRate / DecimationFactor;
        double targetDelta = (2.0 * Math.PI * _tone) / fsLow;
        double radTol = (2.0 * Math.PI * _tol) / fsLow;

        // If your original overlap step was 128 at 48kHz, 
        // it is now ~12.8 at 4.8kHz. 
        int decimatedStepSize = 128 / DecimationFactor;
        double phaseForNextBlock = _lastPhase;

        for (int i = 0; i < samples.Length; i++)
        {
            // 1. Accumulate for the Average (Simple Low Pass)
            _decimationSum += samples[i];
            _decimationCount++;

            // 2. Every 10th sample, run the Hilbert/Phase Logic
            if (_decimationCount >= DecimationFactor)
            {
                float decimatedSample = _decimationSum / DecimationFactor;
                decimatedSample = samples[i];           //no avg case
                _decimationSum = 0;
                _decimationCount = 0;

                // Run Hilbert at 4.8kHz
                var (I, Q) = _hilbert.GetIQ(decimatedSample);
                double magnitude = Math.Sqrt(I * I + Q * Q);
                double currentPhase = Math.Atan2(Q, I);

                // Calculate Delta
                double deltaPhase = currentPhase - _lastPhase;
                while (deltaPhase <= -Math.PI) deltaPhase += 2 * Math.PI;
                while (deltaPhase > Math.PI) deltaPhase -= 2 * Math.PI;

                _lastPhase = currentPhase;

                // Fuzzy Gate Logic
                double phaseError = Math.Abs(deltaPhase - targetDelta);
                double confidence = 0;

                if (phaseError < radTol)
                    confidence = 1.0;
                else if (phaseError < radTol * 2.5)
                    confidence = 1.0 - ((phaseError - radTol) / (radTol * 1.5));

                sumConfidence += confidence;
                sumMag += magnitude * confidence;
                decimatedProcessedCount++;

                // Track phase for the overlap handoff
                if (decimatedProcessedCount == decimatedStepSize)
                {
                    phaseForNextBlock = currentPhase;
                }
            }
        }

        if (decimatedProcessedCount > 0)
        {
            _lastBlockConfidence = sumConfidence / decimatedProcessedCount;
            _lastPhase = phaseForNextBlock;

            // Final magnitude scaling
            return (sumMag / decimatedProcessedCount) * 50000 / _scaling;
        }

        return 0;
    }

    /* original 48kHz version
    public override double ProcessBlock(float[] samples)
    {
        double sumConfidence = 0;
        double sumMag = 0;
        float[] cleanSamples = samples;     // _preFilter.Apply(samples);
        double targetDelta = (2.0 * Math.PI * _tone) / ((double)_sampleRate);
        double radTol = (2.0 * Math.PI * _tol) / ((double)_sampleRate);
        int stepSize = 128;     //******  needs to match the 50% overlap block size
        double _phaseForNextBlock = 0;

        for (int i = 0; i < cleanSamples.Length; i++)
        {
            var (I, Q) = _hilbert.GetIQ(cleanSamples[i]);
            double magnitude = Math.Sqrt(I * I + Q * Q);

            double currentPhase = Math.Atan2(Q, I);
            double deltaPhase = currentPhase - _lastPhase;
            _lastPhase = currentPhase;

            // Normalization & Fuzzy Gate logic
            while (deltaPhase <= -Math.PI) deltaPhase += 2 * Math.PI;
            while (deltaPhase > Math.PI) deltaPhase -= 2 * Math.PI;

            double phaseError = Math.Abs(deltaPhase - targetDelta);
            double confidence = 0;

            if (phaseError < radTol) confidence = 1.0;
            else if (phaseError < radTol * 2.5)
                confidence = 1.0 - ((phaseError - radTol) / (radTol * 1.5));

            sumConfidence += confidence;
            sumMag += magnitude * confidence;

            if (i == stepSize - 1)
            {
                _phaseForNextBlock = currentPhase;
            }
        }
        _lastBlockConfidence = sumConfidence / cleanSamples.Length;
        _lastPhase = _phaseForNextBlock;
        return (sumMag / cleanSamples.Length) * 50000 / _scaling;
    }*/




    public class HilbertFIR
    {
        private readonly float[] _taps;
        private readonly float[] _buffer;
        private int _bufferIndex = 0;
        private readonly int _delay;

        public HilbertFIR(int t = 63)
        {


            // Use these parameters for the 4.8kHz test
            int taps = 63; // At 4.8kHz, 63 taps is plenty (approx 13ms of data)
            double fsLow = 4800.0;
            int mid = (taps - 1) / 2;

            _taps = new float[taps];
            _buffer = new float[taps];
            _delay = (taps - 1) / 2; // Exact center tap for symmetry

            for (int i = 0; i < taps; i++)
            {
                int n = i - mid;
                if (n == 0 || n % 2 == 0)
                {
                    _taps[i] = 0;
                }
                else
                {
                    // Pure Hilbert Coefficient
                    _taps[i] = (float)(2.0 / (Math.PI * n));
                }
                // NO WINDOW for this test - let's see the raw phase accuracy
            }
        }

        /*
        public HilbertFIR(int n = 127)
        {
            _taps = new float[n];
            _buffer = new float[n];
            _delay = (n - 1) / 2; // Exact center tap for symmetry

            for (int i = 0; i < n; i++)
            {
                int mid = i - _delay;
                if (mid == 0 || mid % 2 == 0) _taps[i] = 0;
                else
                {
                    _taps[i] = (float)(2.0 / (Math.PI * mid));
                    // Blackman Window for better stop-band rejection
                    //double a0 = 0.42, a1 = 0.5, a2 = 0.08;
                    //double window = a0 - a1 * Math.Cos(2 * Math.PI * i / (n - 1)) + a2 * Math.Cos(4 * Math.PI * i / (n - 1));

                    // Hamming Window instead of Blackman
                    double window = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (n - 1));
                    _taps[i] *= (float)window;
                }
            }
        }*/

        public (float I, float Q) GetIQ(float sample)
        {
            // 1. Put the new sample in
            _buffer[_bufferIndex] = sample;

            // 2. Calculate Q (The filter center is mid)
            float Q = 0;
            int mid = (_taps.Length - 1) / 2;
            for (int i = 0; i < _taps.Length; i++)
            {
                int idx = (_bufferIndex - i + _taps.Length) % _taps.Length;
                Q += _taps[i] * _buffer[idx];
            }

            // 3. Pull I from the EXACT SAME buffer state
            int iIdx = (_bufferIndex - mid + _taps.Length) % _taps.Length;
            float I = _buffer[iIdx];

            // 4. Update the index ONLY AFTER both are pulled
            _bufferIndex = (_bufferIndex + 1) % _taps.Length;

            return (I, Q);
        }

        /*
        public (float I, float Q) GetIQ(float sample)
        {
            _buffer[_bufferIndex] = sample;
            float Q = 0;
            for (int i = 0; i < _taps.Length; i++)
            {
                int idx = (_bufferIndex - i + _taps.Length) % _taps.Length;
                Q += _taps[i] * _buffer[idx];
            }

            // Ensure I is pulled from the EXACT middle of the window used for Q
            int mid = (_taps.Length - 1) / 2;
            int iIdx = (_bufferIndex - mid + _buffer.Length) % _buffer.Length;
            float I = _buffer[iIdx];

            /*
            // Delay the real component to align with the Q shift center
            int delayIdx = (_bufferIndex - _delay + _buffer.Length) % _buffer.Length;
            float I = _buffer[delayIdx];
            //

            if (++_bufferIndex >= _buffer.Length) _bufferIndex = 0;
            return (I, Q);
        }
    */
    }
}
