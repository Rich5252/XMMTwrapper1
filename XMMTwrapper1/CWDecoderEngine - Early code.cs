using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

public class CWDecoderEngine
{
    //needs to be in a single thread task to get sequencial data blocks
    private readonly BlockingCollection<byte[]> _audioQueue = new BlockingCollection<byte[]>();

    private WaveInEvent _waveIn;
    private GoertzelFilter _filter;
    public Action<double> OnMagnitudeUpdated; // For VU meters
    public Action<bool> OnToneDetected;      // For the Dot/Dash logic
    private AdaptiveThreshold _adaptor = new AdaptiveThreshold();
    
    private MorseClock _clock = new MorseClock();
    // Link the clock's internal events to the Engine's public events
    // ... inside OnDataAvailable ...
    // bool isTonePresent = _adaptor.IsSignalPresent(mag);
    // _clock.UpdateState(isTonePresent);

    public Action<string> OnSequenceChanged;
    public Action<char> OnCharacterDecoded;

    // Adjustable Parameters
    public double TargetFrequency { get; set; } = 1000.0;
    public double Threshold { get; set; } = 0.05; // We will automate this later

    // Squelch - Adjust this based on your Goertzel output. 
    // If your mag is 0.0 to 1.0, 0.05 is a good start.
    public double SquelchLevel { get; set; } = 0.05;

    public CWDecoderEngine()
    {
        //start one (and only one) processing thread
        Task.Factory.StartNew(() => ProcessAudioLoop(), TaskCreationOptions.LongRunning);

        // THIS IS THE FIX: Move the stitching inside the constructor
        _clock.OnCharacterDecoded = (c) => OnCharacterDecoded?.Invoke(c);
        _clock.OnSequenceChanged = (s) => OnSequenceChanged?.Invoke(s);

        // Initialize your filter here too if needed
        //_filter = new GoertzelFilter(1000, 44100, 512);
    }

    private void ProcessAudioLoop()
    {
        foreach (var buffer in _audioQueue.GetConsumingEnumerable())
        {
            float[] samples = ConvertToFloat(buffer, buffer.Length);
            double mag = _filter.CalculateMagnitude(samples);
            bool isTone = _adaptor.IsSignalPresent(mag);

            // 1. UPDATE LEVEL BAR (Always fire)
            OnMagnitudeUpdated?.Invoke((int)(mag * 100));

            // 2. UPDATE UI INDICATOR (Fire the raw state so the user sees the "flicker")
            // This is the call you likely lost!
            OnToneDetected?.Invoke(isTone);

            // 3. UPDATE CLOCK (The Clock handles its own debouncing)
            _clock.UpdateState(isTone);
        }
    }

    public void Start(int deviceIndex = 0)
    {
        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceIndex,
            WaveFormat = new WaveFormat(44100, 1) // 44.1kHz Mono
        };

        // Initialize Filter: 512 sample block size is good for CW timing
        _filter = new GoertzelFilter(TargetFrequency, 44100, 512);

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.BufferMilliseconds = 20; // High resolution for CW
        _waveIn.StartRecording();
    }

    private double _smoothedMag = 0;            //level smoothing

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // Just grab the bytes and drop them in the bucket
        byte[] data = new byte[e.BytesRecorded];
        Array.Copy(e.Buffer, data, e.BytesRecorded);
        _audioQueue.Add(data);
    }

    public void Stop()
    {
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
    }

    public void SetWPM(int wpm)
    {
        // 1200 / WPM = Dot length in milliseconds
        _clock.SetDotLength(1200 / wpm);
    }

    private float[] ConvertToFloat(byte[] buffer, int bytesRecorded)
    {
        // 16-bit PCM uses 2 bytes per sample.
        int sampleCount = bytesRecorded / 2;
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            // Extract the two bytes and combine into a 16-bit signed integer (short)
            // This handles the Little-Endian format used by Windows/WAV
            short sample = (short)((buffer[i * 2 + 1] << 8) | buffer[i * 2]);

            // Normalize the 16-bit value (-32768 to 32767) to a float (-1.0 to 1.0)
            samples[i] = sample / 32768f;
        }

        return samples;
    }

    private void ProcessBuffer(byte[] buffer, int bytesRecorded)
    {
        ReadOnlySpan<byte> source = buffer.AsSpan(0, bytesRecorded);

        // You can process the bytes directly inside the loop 
        // without creating a 'float[] samples' array first.
        for (int i = 0; i < bytesRecorded; i += 2)
        {
            short sample = BitConverter.ToInt16(source.Slice(i, 2));
            float fSample = sample / 32768f;

            // Pass fSample directly into your filter logic here...
        }
    }
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
        double magnitude = Math.Sqrt(Math.Max(0, power));       // linear mag
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

    public bool IsSignalPresentV1(double currentMagnitude)
    {
        // 1. THE SQUELCH: If it's too quiet, it's definitely not a tone.
        if (currentMagnitude < SquelchLevel)
        {
            return false;
        }

        // 1. Update Noise Floor (slowly follows the minimums)
        if (currentMagnitude < _noiseFloor || _noiseFloor == 0)
            _noiseFloor = currentMagnitude;
        else
            _noiseFloor = (_noiseFloor * 0.999) + (currentMagnitude * 0.001);

        // 2. Update Peak Level (follows the maximums)
        if (currentMagnitude > _peakLevel)
            _peakLevel = currentMagnitude;
        else
            _peakLevel *= NoiseRelease; // Decay peaks slowly

        // 3. Calculate Dynamic Threshold 
        // We set the trigger at roughly 30% of the way between noise and peak
        double range = _peakLevel - _noiseFloor;
        double threshold = _noiseFloor + (range * 0.3);

        //return currentMagnitude > threshold && range > 0.001; // Ensure there's a real signal

        //add hysterisis
        double highThreshold = _noiseFloor + (range * 0.4); // Trigger point
        double lowThreshold = _noiseFloor + (range * 0.2);  // Release point

        if (!_isCurrentlyToning && currentMagnitude > highThreshold)
        {
            _isCurrentlyToning = true;
        }
        else if (_isCurrentlyToning && currentMagnitude < lowThreshold)
        {
            _isCurrentlyToning = false;
        }

        return _isCurrentlyToning;
    }
}