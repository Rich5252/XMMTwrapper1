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

    public double TargetFrequency { get; set; } = 750.0;

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

    public GoertzelFilter(double targetFreq, int sampleRate, int blockSize)
    {
        _targetFreq = targetFreq;
        _sampleRate = sampleRate;
        _blockSize = blockSize;

        // Calculate the coefficient
        double k = (int)(0.5 + ((_blockSize * _targetFreq) / _sampleRate));
        double omega = (2.0 * Math.PI * k) / _blockSize;
        _coeff = 2.0 * Math.Cos(omega);
    }

    public double CalculateMagnitude(float[] samples)
    {
        double q0 = 0, q1 = 0, q2 = 0;

        foreach (var sample in samples)
        {
            q0 = (_coeff * q1) - q2 + sample;
            q2 = q1;
            q1 = q0;
        }

        double power = (q1 * q1 + q2 * q2 - q1 * q2 * _coeff);
        double magnitude = Math.Sqrt(Math.Max(0, power)) / 100;       // linear mag
        return magnitude;

        // Return the squared magnitude (faster than taking Square Root)
        //return (q1 * q1) + (q2 * q2) - (q1 * q2 * _coeff);
    }
}

public class AdaptiveThreshold
{
    private double _noiseFloor = 0.0;
    private double _peakLevel = 0.01; // Start with a small non-zero peak
    private bool _isToning = false;

    public double SquelchLevel { get; set; } = 0.01;

    public bool IsSignalPresent(double mag)
    {
        // 1. THE SQUELCH: If it's too quiet, it's definitely not a tone.
        if (mag < SquelchLevel)
        {
            _peakLevel *= 0.98; // Decay the peak slowly when silent to adapt to fading
            _isToning = false;
            return false;
        }

        // 2. DYNAMIC TRACKING (This was missing!)
        // Quickly follow peaks, slowly follow the noise floor
        if (mag > _peakLevel) _peakLevel = mag;
        else _peakLevel = (_peakLevel * 0.99) + (mag * 0.01);

        if (mag < _noiseFloor || _noiseFloor == 0) _noiseFloor = mag;
        else _noiseFloor = (_noiseFloor * 0.999) + (mag * 0.001);

        // 3. HYSTERESIS LOGIC
        double range = _peakLevel - _noiseFloor;
        double upperThreshold = _noiseFloor + (range * 0.4);
        double lowerThreshold = _noiseFloor + (range * 0.2);

        if (!_isToning && mag > upperThreshold)
        {
            _isToning = true;
        }
        else if (_isToning && mag < lowerThreshold)
        {
            _isToning = false;
        }

        return _isToning;
    }
}