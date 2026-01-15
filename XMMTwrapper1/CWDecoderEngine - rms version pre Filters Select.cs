using NAudio.Wave;
using System.Collections.Concurrent;


public class CWDecoderEngine
{
    private bool _isRunning = false;
    private readonly BlockingCollection<byte[]> _audioQueue = new BlockingCollection<byte[]>();
    private List<float> _residualBuffer = new List<float>();
    private WaveInEvent _waveIn;
    private GoertzelFilter _filter;
    private AdaptiveThreshold _adaptor = new AdaptiveThreshold();
    private MorseClock _clock = new MorseClock();

    public Action<double> OnMagnitudeUpdated;
    public Action<bool> OnToneDetected;
    public Action<string> OnSequenceChanged;
    public Action<char> OnCharacterDecoded;

    public double TargetFrequency { get; set; } = 1000.0;
    public double InputScaleFactor { get; set; } = 1000.0;

    public CWDecoderEngine()
    {
        Task.Factory.StartNew(() => ProcessAudioLoop(), TaskCreationOptions.LongRunning);
        _clock.OnCharacterDecoded = (c) => OnCharacterDecoded?.Invoke(c);
        _clock.OnSequenceChanged = (s) => OnSequenceChanged?.Invoke(s);

        EnableLogging("C:\\Users\\office2\\Documents\\Ham Radio\\VScode\\XMMTwrapper_C#\\XMMTwrapper1\\CWlogTemporalFilter.csv");
    }

    public void Start(int deviceIndex = 0)
    {
        if (_isRunning) return;

        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceIndex,
            WaveFormat = new WaveFormat(48000, 16, 1),
            BufferMilliseconds = 10
        };

        int rate = _waveIn.WaveFormat.SampleRate;
        _filter = new GoertzelFilter(TargetFrequency, rate, 256);
        _clock.SetSampleRate(rate);

        _waveIn.DataAvailable += (s, e) => {
            if (!_audioQueue.IsAddingCompleted)
            {
                byte[] data = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, data, e.BytesRecorded);
                _audioQueue.Add(data);
            }
        };

        _waveIn.StartRecording();
        _isRunning = true;
    }

    public void Stop()
    {
        if (!_isRunning) return;

        // 1. Stop the soundcard
        _waveIn?.StopRecording();

        // 2. Tell the BlockingCollection no more data is coming
        // This allows the ProcessAudioLoop 'foreach' to exit naturally
        _audioQueue.CompleteAdding();

        _isRunning = false;
    }

    public void Dispose()
    {
        Stop();
        _waveIn?.Dispose();
        _audioQueue?.Dispose();
    }

    //50% overlap version
    private void ProcessAudioLoop()
    {
        foreach (var buffer in _audioQueue.GetConsumingEnumerable())
        {
            float[] newSamples = ConvertToFloat(buffer);
            _residualBuffer.AddRange(newSamples);

            // Slide window by 128 samples (50% overlap of a 256 block)
            while (_residualBuffer.Count >= 256)
            {
                // 1. Get the current 256 block
                float[] block = _residualBuffer.GetRange(0, 256).ToArray();

                // 2. SLIDE: Only remove 128 samples, not 256
                _residualBuffer.RemoveRange(0, 128);

                // 3. Process
                double mag = _filter.CalculateMagnitude(block, 1);
                bool isTone = _adaptor.IsSignalPresent(mag);

                // Inside ProcessAudioLoop, after IsSignalPresent(mag):
                if (_csvLogger != null)
                {
                    // Record the decision-making data
                    _csvLogger.WriteLine($"{_clock.GetTotalSamples()},{mag:F6},{_adaptor.GetCurrentThreshold():F6},{isTone},{(isTone ? 1 : 0)}");
                }

                OnMagnitudeUpdated?.Invoke(mag);
                OnToneDetected?.Invoke(isTone);

                // 4. Update Clock with ONLY 128 samples of "time" 
                // because we are now pulsing the clock twice as often
                _clock.UpdateState(isTone, 128);
            }
        }
    }

    /*//None overlap version
    private void ProcessAudioLoop()
    {
        foreach (var buffer in _audioQueue.GetConsumingEnumerable())
        {
            _filter.InputScaleFactor = InputScaleFactor;            //update input level adjust from UI
            float[] newSamples = ConvertToFloat(buffer);
            _residualBuffer.AddRange(newSamples);

            // Process strictly in 256-sample blocks
            while (_residualBuffer.Count >= 256)
            {
                float[] block = _residualBuffer.GetRange(0, 256).ToArray();
                _residualBuffer.RemoveRange(0, 256);

                double mag = _filter.CalculateMagnitude(block);
                bool isTone = _adaptor.IsSignalPresent(mag);

                OnMagnitudeUpdated?.Invoke(mag);
                OnToneDetected?.Invoke(isTone);

                // Feed the clock EXACTLY 256 samples of "time"
                _clock.UpdateState(isTone, 256);
            }
        }
    }
    */

    private float[] ConvertToFloat(byte[] buffer)
    {
        int bytesPerSample = 2; // 16-bit
        int channels = _waveIn.WaveFormat.Channels;
        int monoSampleCount = buffer.Length / (bytesPerSample * channels);
        float[] samples = new float[monoSampleCount];

        for (int i = 0; i < monoSampleCount; i++)
        {
            int offset = i * bytesPerSample * channels;
            short s = (short)((buffer[offset + 1] << 8) | buffer[offset]);
            samples[i] = s / 32768f;
            // Note: If stereo, this ignores the second channel to save CPU. 
            // For CW, one channel is usually sufficient.
        }
        return samples;
    }

    public void SetWPM(int wpm) => _clock.SetDotLength(1200.0 / wpm);

    private StreamWriter _csvLogger;
    public void EnableLogging(string filePath)
    {
        _csvLogger = new StreamWriter(filePath);
        _csvLogger.WriteLine("Timestamp,Magnitude,Threshold,IsToning,State");
    }

}


public class GoertzelFilter
{
    private double _coeff;
    private double _targetFreq;
    private int _sampleRate;
    private int _blockSize;

    public double InputScaleFactor { get; set; } = 1000;
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

    public double CalculateMagnitude(float[] samples, int typ)
    {
        double q0 = 0, q1 = 0, q2 = 0, power = 0;

        if (typ == 0)       //GoertzelFilter
        {
            // Apply Hann windowing DURING the iteration
            for (int i = 0; i < samples.Length && i < _blockSize; i++)
            {
                // Taper the sample before adding to the filter state
                float windowedSample = samples[i] * _hannWindow[i];

                q0 = (_coeff * q1) - q2 + windowedSample;
                q2 = q1;
                q1 = q0;
            }

            power = (q1 * q1 + q2 * q2 - q1 * q2 * _coeff) / InputScaleFactor;
            //double magnitude = Math.Sqrt(Math.Max(0, power))       // linear mag
            //return magnitude;
        }
        if (typ == 1)        //simple sum of squares with hann
        {
            for (int i = 0; i < samples.Length && i < _blockSize; i++)
            {
                float windowedSample = samples[i] * _hannWindow[i];
                power = power + (windowedSample * windowedSample);
            }
            power = power * 100000 / InputScaleFactor;
        }

        // Return the squared magnitude (faster than taking Square Root)
        return power;
    }
}

//With temporal drop out filter
public class AdaptiveThreshold
{
    private double _noiseFloor = 0.0;
    private double _peakLevel = 0.01;
    private bool _isToning = false;

    // DEBOUNCE STATE
    private int _consecutiveHits = 0;
    private int _consecutiveMisses = 0;
    private const int DebounceLimit = 3; // ~8ms at 50% overlap/48kHz

    public double SquelchLevel { get; set; } = 0.01;

    public bool IsSignalPresent(double mag)
    {
        // 1. Dynamic tracking (Peak and Noise)
        if (mag > SquelchLevel)
        {
            if (mag > _peakLevel) _peakLevel = mag;
            else _peakLevel = (_peakLevel * 0.98) + (mag * 0.02);
        }

        if (mag < _noiseFloor || _noiseFloor == 0) _noiseFloor = mag;
        else _noiseFloor = (_noiseFloor * 0.99) + (mag * 0.01);

        double range = _peakLevel - _noiseFloor;

        // 2. WIDER HYSTERESIS
        // Your log showed threshold climbing to 0.20 when peak was 0.23. 
        // That's too high. We want a lower "exit" bar.
        double upperThreshold = _noiseFloor + (range * 0.60); // Must reach 60% to start
        double lowerThreshold = _noiseFloor + (range * 0.25); // Must drop below 25% to stop

        bool meetsThreshold = _isToning ? (mag > lowerThreshold) : (mag > upperThreshold);

        // 3. DEBOUNCE LOGIC
        if (meetsThreshold && mag > SquelchLevel)
        {
            _consecutiveHits++;
            _consecutiveMisses = 0;
            if (_consecutiveHits >= DebounceLimit) _isToning = true;
        }
        else
        {
            _consecutiveMisses++;
            _consecutiveHits = 0;
            // Only turn off if we've seen 3 consecutive "bad" windows
            if (_consecutiveMisses >= DebounceLimit) _isToning = false;
        }

        return _isToning;
    }

    // Helpers for your CSV Logger
    public double GetCurrentThreshold() => _noiseFloor + (_peakLevel - _noiseFloor) * (_isToning ? 0.25 : 0.60);
    public double GetPeak() => _peakLevel;
    public double GetNoise() => _noiseFloor;
}

/*//No drop out filter
public class AdaptiveThreshold
{
    private double _noiseFloor = 0.0;
    private double _peakLevel = 0.01;
    private bool _isToning = false;

    public double SquelchLevel { get; set; } = 0.01;
    public double GetCurrentThreshold() => _noiseFloor + (_peakLevel - _noiseFloor) * 0.4;

    public bool IsSignalPresent(double mag)
    {
        if (mag < SquelchLevel)
        {
            // Faster decay (0.95) helps separate 40WPM dots
            _peakLevel *= 0.95;
            _isToning = false;
            return false;
        }

        // Fast attack for high-speed onset
        if (mag > _peakLevel) _peakLevel = mag;
        else _peakLevel = (_peakLevel * 0.98) + (mag * 0.02);

        if (mag < _noiseFloor || _noiseFloor == 0) _noiseFloor = mag;
        else _noiseFloor = (_noiseFloor * 0.99) + (mag * 0.01);

        double range = _peakLevel - _noiseFloor;

        // Tighter hysteresis for rapid 30ms transitions
        double upperThreshold = _noiseFloor + (range * 0.5);
        double lowerThreshold = _noiseFloor + (range * 0.2);

        if (!_isToning && mag > upperThreshold) _isToning = true;
        else if (_isToning && mag < lowerThreshold) _isToning = false;

        return _isToning;
    }
}*/