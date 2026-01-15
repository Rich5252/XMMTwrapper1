using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class CWDecoderEngine
{
    private readonly BlockingCollection<byte[]> _audioQueue = new BlockingCollection<byte[]>();
    private WaveInEvent _waveIn;
    private GoertzelFilter _filter;
    private AdaptiveThreshold _adaptor = new AdaptiveThreshold();
    private MorseClock _clock = new MorseClock();

    public Action<double> OnMagnitudeUpdated;
    public Action<bool> OnToneDetected;
    public Action<string> OnSequenceChanged;
    public Action<char> OnCharacterDecoded;

    public double TargetFrequency { get; set; } = 1000.0;
    public double SquelchLevel { get => _adaptor.SquelchLevel; set => _adaptor.SquelchLevel = value; }

    public CWDecoderEngine()
    {
        // Start the single consumer thread
        Task.Factory.StartNew(() => ProcessAudioLoop(), TaskCreationOptions.LongRunning);

        _clock.OnCharacterDecoded = (c) => OnCharacterDecoded?.Invoke(c);
        _clock.OnSequenceChanged = (s) => OnSequenceChanged?.Invoke(s);
    }

    private void ProcessAudioLoop()
    {
        foreach (var buffer in _audioQueue.GetConsumingEnumerable())
        {
            float[] samples = ConvertToFloat(buffer, buffer.Length);
            double mag = _filter.CalculateMagnitude(samples);

            // Adaptive threshold handles Hysteresis
            bool isTone = _adaptor.IsSignalPresent(mag);

            OnMagnitudeUpdated?.Invoke(mag);
            OnToneDetected?.Invoke(isTone);

            _clock.UpdateState(isTone);
        }
    }

    public void Start(int deviceIndex = 0)
    {
        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceIndex,
            WaveFormat = new WaveFormat(44100, 1),
            BufferMilliseconds = 10 // Low latency for timing accuracy
        };

        _filter = new GoertzelFilter(TargetFrequency, 44100, 256); // Snappier N=256
        _waveIn.DataAvailable += (s, e) => {
            byte[] data = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, data, e.BytesRecorded);
            _audioQueue.Add(data);
        };
        _waveIn.StartRecording();
    }

    public void SetWPM(int wpm) => _clock.SetDotLength(1200.0 / wpm);

    private float[] ConvertToFloat(byte[] buffer, int bytesRecorded)
    {
        int sampleCount = bytesRecorded / 2;
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            short sample = (short)((buffer[i * 2 + 1] << 8) | buffer[i * 2]);
            samples[i] = sample / 32768f;
        }
        return samples;
    }

    public void Stop() { _waveIn?.StopRecording(); _waveIn?.Dispose(); }
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
        double magnitude = Math.Sqrt(Math.Max(0, power)) / 1000;       // linear mag
        return magnitude;

        // Return the squared magnitude (faster than taking Square Root)
        //return (q1 * q1) + (q2 * q2) - (q1 * q2 * _coeff);
    }
}

public class AdaptiveThreshold
{
    private double _noiseFloor = 0.0;
    private double _peakLevel = 0.0;

    // Adjust these to change how fast the threshold reacts to changes
    private const double NoiseRelease = 0.95;
    private const double PeakAttack = 0.1;
    private bool _isCurrentlyToning = false;

    // Squelch - Adjust this based on your Goertzel output. 
    // If your mag is 0.0 to 1.0, 0.05 is a good start.
    public double SquelchLevel { get; set; } = 0.01;

    private bool _isToning = false;
    private double minMag = 1000.0;
    private double maxMag = 0.0;

    public bool IsSignalPresent(double mag)
    {
        if (mag < minMag)
        {
            minMag = mag;
            Log.Mark("MinMag = " + minMag);
        }
        if (mag > maxMag)
        {
            maxMag = mag;
            Log.Mark("maxMag = " + maxMag);
        }

        // 1. THE SQUELCH: If it's too quiet, it's definitely not a tone.
        if (mag < SquelchLevel)
        {
            return false;
        }

        double upperThreshold = _noiseFloor + (_peakLevel - _noiseFloor) * 0.4;
        double lowerThreshold = _noiseFloor + (_peakLevel - _noiseFloor) * 0.2;

        if (!_isToning && mag > upperThreshold)
        {
            _isToning = true; // Hard lock ON
        }
        else if (_isToning && mag < lowerThreshold)
        {
            _isToning = false; // Hard lock OFF
        }

        return _isToning;
    }
}