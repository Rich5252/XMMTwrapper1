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

    public double CalculateMagnitude(float[] samples)
    {
        double q0 = 0, q1 = 0, q2 = 0;

        // Apply Hann windowing DURING the iteration
        for (int i = 0; i < samples.Length && i < _blockSize; i++)
        {
            // Taper the sample before adding to the filter state
            float windowedSample = samples[i] * _hannWindow[i];

            q0 = (_coeff * q1) - q2 + windowedSample;
            q2 = q1;
            q1 = q0;
        }

        double power = (q1 * q1 + q2 * q2 - q1 * q2 * _coeff) / InputScaleFactor;
        //double magnitude = Math.Sqrt(Math.Max(0, power))       // linear mag
        //return magnitude;

        // Return the squared magnitude (faster than taking Square Root)
        return power;
    }
}

public class AdaptiveThreshold
{
    private double _noiseFloor = 0.0;
    private double _peakLevel = 0.01;
    private bool _isToning = false;

    public double SquelchLevel { get; set; } = 0.01;

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
        double upperThreshold = _noiseFloor + (range * 0.45);
        double lowerThreshold = _noiseFloor + (range * 0.25);

        if (!_isToning && mag > upperThreshold) _isToning = true;
        else if (_isToning && mag < lowerThreshold) _isToning = false;

        return _isToning;
    }
}