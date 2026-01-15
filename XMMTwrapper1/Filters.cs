
//**********************************************
// Filter generic Interface
// Structured to call Filter required by Name
// All filters have a "Strategy" configuration call
// All filters use ProcessBlock(float[] samples); and return a scalar level result
//**********************************************
public interface ICWFilter
{
    // Returns a magnitude value (0.0 to 1.0+) based on the input block
    double ProcessBlock(float[] samples);
    string Name { get; }

    double TargetFrequency { get; set; }
    double ScalingFactor { get; set; }

}



//***************************************************
//Goertzel
//***************************************************
public class GoertzelStrategy : ICWFilter
{
    private GoertzelFilter _filter;
    public string Name => "Goertzel";
    
    public double ScalingFactor 
        {
            get => _ScalingFactor;
            set { _ScalingFactor = value; }
        }
    public double TargetFrequency
    {
        get => _freq;
        set { _freq = value; Rebuild(); }
    }

    private double _freq = 1000;                //default Filter target freq
    private int _samplerate = 48000;
    private int _blocksize = 256;
    private double _ScalingFactor = 1000;

    private void Rebuild()
    {
        // 48k rate, new freq, 200Hz bw, 127 taps
        _filter = new GoertzelFilter(TargetFrequency, _samplerate, _blocksize);
    }

    public GoertzelStrategy(double targetFreq, int sampleRate, int blockSize)
    {
        _freq = targetFreq;
        _samplerate = sampleRate;
        _blocksize = blockSize;
        _filter = new GoertzelFilter(targetFreq, sampleRate, blockSize);
    }

    public double ProcessBlock(float[] samples)
    {
        return _filter.CalculateMagnitude(samples) * 77 / _ScalingFactor;
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
        double k = (int)(0.5 + ((_blockSize * _targetFreq) / _sampleRate));
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
public class FIRStrategy : ICWFilter
{
    private FIRFilter _filter;
    public string Name => "FIR Bandpass";
    public double ScalingFactor { get; set; } = 200;
    public double TargetFrequency
    {
        get => _freq;
        set { _freq = value; Rebuild(); }
    }
    public double filterBW
    {
        get => _bw;
        set { _bw = value; Rebuild(); }
    }

    private double _freq = 1000;                //default Filter target freq
    private int _samplerate = 48000;
    private int _taps = 127;
    private double _bw = 200;

    private void Rebuild()
    {
        // 48k rate, new freq, 200Hz bw, 127 taps
        _filter = new FIRFilter(_samplerate, _freq, _bw,  _taps);
    }

    public FIRStrategy(int sampleRate, double centerFreq, double bw, int taps)
    {
        _freq = centerFreq;
        _taps = taps;
        _samplerate = sampleRate;
        _bw = bw;

        _filter = new FIRFilter(sampleRate, centerFreq, bw, taps);
    }


    //
    public double ProcessBlock(float[] samples)
    {
        double sumAbs = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float filtered = _filter.Process(samples[i]);
            sumAbs += Math.Abs(filtered);
        }
        return sumAbs / samples.Length * 50000 / ScalingFactor;
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

//***********************************************
// FIR S/N
//***********************************************
public class FIRSNStrategy : ICWFilter
{
    private FIRSNFilter _filter;
    public string Name => "S/N";
    public double ScalingFactor { get; set; } = 1000;
    public double TargetFrequency
    {
        get => _freq;
        set { _freq = value; Rebuild(); }
    }
    public double filterBW
    {
        get => _bw;
        set { _bw = value; Rebuild(); }
    }

    private double _freq = 1000;                //default Filter target freq
    private int _samplerate = 48000;
    private int _taps = 255;
    private double _bw = 75;

    private void Rebuild()
    {
        // 48k rate, new freq, 200Hz bw, 127 taps
        _filter = new FIRSNFilter(_samplerate, _freq, _bw, _taps);
    }

    public FIRSNStrategy(int sampleRate, double centerFreq, double bw, int taps)
    {
        _freq = centerFreq;
        _taps = taps;
        _samplerate = sampleRate;
        _bw = bw;

        _filter = new FIRSNFilter(sampleRate, centerFreq, bw, taps);
    }


    //
    public double ProcessBlock(float[] samples)
    {
        double sumAbs = 0;
        double sumRaw = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float filtered = _filter.Process(samples[i]);
            sumAbs += Math.Abs(filtered);
            sumRaw += Math.Abs(samples[i]);
        }
        double FIRoutputLevel = sumAbs / samples.Length * 50000 / ScalingFactor;
        sumRaw = sumRaw / samples.Length * 7777 / ScalingFactor;
        //double SNratio = 20 * Math.Log10(FIRoutputLevel / sumRaw) * 1000 / ScalingFactor;
        double noise = Math.Pow(sumRaw, 2) - Math.Pow(FIRoutputLevel, 2);
        double threshold = 0.0001;
        if (noise < threshold) noise = threshold;
        double SNratio = Math.Pow(FIRoutputLevel, 2) / noise / ScalingFactor;
        return SNratio;
    }
}

public class FIRSNFilter
{
    private readonly float[] _taps;
    private readonly float[] _buffer;
    private int _bufferIndex = 0;

    public FIRSNFilter(int sampleRate, double centerFreq, double bandwidth, int tapCount = 127)
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
public class RMSStrategy : ICWFilter
{
    private RMSFilter _filter;
    public string Name => "RMS";
    public double ScalingFactor { get; set; } = 1000;
    public double TargetFrequency
    {
        get => _freq;
        set { _freq = value; Rebuild(); }
    }
    public double filterBW
    {
        get => _bw;
        set { _bw = value; Rebuild(); }
    }

    private double _freq = 1000;                //default Filter target freq
    private int _samplerate = 48000;
    private int _size = 256;
    private double _bw = 200;

    private void Rebuild()
    {
        // 48k rate, new freq, 200Hz bw, 127 taps
        _filter = new RMSFilter(_samplerate, _size);
    }

    public RMSStrategy(int sampleRate, int size)
    {
        _samplerate = sampleRate;
        _size = size;
        _filter = new RMSFilter(sampleRate, size);
    }

    public double ProcessBlock(float[] samples)
    {
        return _filter.CalculateMagnitude(samples) / 18.7 / ScalingFactor;
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
        double k = (int)(0.5 + ((_blockSize * _targetFreq) / _sampleRate));
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
