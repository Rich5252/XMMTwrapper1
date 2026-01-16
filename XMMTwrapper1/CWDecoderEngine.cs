//**********************************************************
// STRATEGY: ADAPTIVE ENVELOPE TRACKING & TEMPORAL FILTERING
//**********************************************************
// Combines statistical smoothing with dynamic environment profiling to extract weak signals.
//
//Key Parameter Definitions (see MorseTuningConfig.cs for latest values):
//
// PROCESSING CHAIN:
// - GAUSSIAN (5-Point): Low-pass envelope smoothing to reduce thermal noise "chatter".
// - MEDIAN (3-Point): Non-linear filter to eliminate impulsive static crashes/spikes.
// - NOISE TRACKER (0.995): Continuously profiles the baseline atmospheric noise.
// - LEAKY PEAK (0.98): Fast-attack/slow-decay detector that rides the signal crests.
//
// DECISION LOGIC:
// - DUAL-THRESHOLD HYSTERESIS: Uses 50% (Upper) to trigger Mark and 20% (Lower) for Space.
// - TEMPORAL DEBOUNCE: Requires 4 consecutive misses (~10ms) to end a tone (heals QRN dips).
// - SNR TRACKING: Power-based ratio of Mean Square magnitudes during Mark vs Space states.
//
// SQUELCH GATE: Forces peak decay to noise floor when idle to prevent "decoder deafness".
//
// FILTERS - adjustable on the fly.
// - "FIR Bandpass" - default and best performance so far. 45Hz bandwidth optimum for 25-30wpm
// - "S/N" - uses FIR bandpass and then computes signal to noise ratio as input to MorseClock
// - "Goertzel" - classic high speed IIR filter, low CPU load but not as good as FIR for noise rejection
// - "RMS" - wideband signal level for comparison or wideband decode, needs good signals
// - "Hilbert Analytic" - signal envelope and edge detection with phase noise rejection 
//
//PERFORMANCE METRICS
// - see PARIS test audio files ~6dB S/N in 45Hz BW
// - FIR gave 98.7% accuracy scored using Levenshtein Benchmark Metric cf 88.6% using Fldigi
//
//****************************************************************************************


using NAudio.Wave;                          //audio support library
using System.Collections.Concurrent;

public class CWDecoderEngine
{
    private bool _isRunning = false;
    private readonly BlockingCollection<byte[]> _audioQueue = new BlockingCollection<byte[]>();
    private List<float> _residualBuffer = new List<float>();
    private WaveInEvent _waveIn;
    private AdaptiveThreshold _adaptor = new AdaptiveThreshold();
    private MorseClock _clock = new MorseClock();

    public Action<double> OnMagnitudeUpdated;
    public Action<double> OnSNratioUpdated;
    public Action<bool> OnToneDetected;
    public Action<string> OnSequenceChanged;
    public Action<char> OnCharacterDecoded;
    public Action<int> OnWpmChanged;

    public double TargetFrequency { get; set; } = 1000.0;
    public double FIRbw { get; set; } = 50.0;           //need to refresh filet to take new value
    private double _InputScalingFactor = 200;

    public void SetInputScale(double _SF)
    {
        _InputScalingFactor = _SF;
        _activeFilter.ScalingFactor = _InputScalingFactor;
    }

    public void SetSquelchLevel(double _level) => _adaptor.SquelchLevel = _level;
    public void SetGaussian(bool bGF) => _adaptor.GaussianFilter = bGF;     //can be set on the fly
    public void SetMedian(bool bM) => _adaptor.GaussianFilter = bM;         //can be set on the fly

    // Thread safety lock
    private readonly object _filterLock = new object();     //used when changing
    private ICWFilter _activeFilter;
    private int _SampleRate = 48000;

    public string LoggingFilePath { get; set; } = "C:\\Users\\office2\\Documents\\Ham Radio\\VScode\\XMMTwrapper_C#\\XMMTwrapper1\\CWlogTemporalFilter.csv";

    // You can swap this at any time
    public void SetFilterStrategy(ICWFilter filter)
    {
        _activeFilter = filter;
        Log.Mark($"Switched to filter: {filter.Name}");
    }

    public CWDecoderEngine()
    {
        Task.Factory.StartNew(() => ProcessAudioLoop(), TaskCreationOptions.LongRunning);
        _clock.OnCharacterDecoded = (c) => OnCharacterDecoded?.Invoke(c);
        _clock.OnSequenceChanged = (s) => OnSequenceChanged?.Invoke(s);
        _clock.OnWpmChanged = (i) => OnWpmChanged?.Invoke(i);
    }

    // start/re-start engine from UI on seleced audio device
    public void Start(int deviceIndex = 0)
    {
        if (_isRunning) return;

        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceIndex,
            WaveFormat = new WaveFormat(48000, 16, 1),
            BufferMilliseconds = 10
        };

        _SampleRate = _waveIn.WaveFormat.SampleRate;
        _clock.SetSampleRate(_SampleRate);

        //_activeFilter = new GoertzelStrategy(TargetFrequency, _SampleRate, 256 /* buffersize */);    //default filter
        _activeFilter = new FIRStrategy(_SampleRate, TargetFrequency, FIRbw /* BW */, 511 /* taps */);    //default filter
        _activeFilter.ScalingFactor = _InputScalingFactor;


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


    int _updateCounter = 0;
    double magSmoothed = 0;
    double SNratio= 0;

    //50% overlap of incoming audio stream version
    //each 256 block is processed every 128 samples at 48kHz Fs (4.166ms per block)
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
                // Unified call regardless of strategy
                double mag = _activeFilter.ProcessBlock(block);         //filter and return scaler magnitude
                bool isTone = _adaptor.IsSignalPresent(mag);
                SNratio = _adaptor.SNratio;                             //pick up the current SN measurement for UI

                // Inside ProcessAudioLoop, after IsSignalPresent(mag):
                if (_csvLogger != null)
                {
                    // Record the decision-making data
                    _csvLogger.WriteLine($"{_clock.GetTotalSamples()},{mag:F6},{_adaptor.GetCurrentThreshold():F6},{isTone},{(isTone ? 1 : 0)}");
                }

                if (magSmoothed == 0) { magSmoothed = mag; }
                else { magSmoothed = magSmoothed * 0.99 + mag * 0.01; }
                _updateCounter++;
                if (_updateCounter >= 50)
                {
                    OnMagnitudeUpdated?.Invoke(magSmoothed);
                    OnToneDetected?.Invoke(isTone);
                    OnSNratioUpdated?.Invoke(SNratio);
                    _updateCounter = 0;
                }

                // 4. Update Clock with ONLY 128 samples of "time" 
                // because we are now pulsing the clock twice as often
                _clock.UpdateState(isTone, 128);
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

    // Diagnostic logging. Call from UI
    private readonly object _logLock = new object();
    private StreamWriter _csvLogger;
    public void StartLogging()
    {
        EnableLogging(LoggingFilePath);
    }
    public void EnableLogging(string filePath)
    {
        lock (_logLock)
        {
            _csvLogger = new StreamWriter(filePath);
            _csvLogger.WriteLine("Timestamp,Magnitude,Threshold,IsToning,State");
        }
    
    }
    public void StopLogging()
    {
        lock (_logLock)
        {
            if (_csvLogger != null)
            {
                _csvLogger.Close();
                _csvLogger?.Dispose();
                _csvLogger = null;
            }
        }
    }

    // UI calls this when the user picks from the dropdown
    public void ChangeFilter(string filterName)
    {
        lock (_filterLock)
        {
            switch (filterName)
            {
                case "FIR Bandpass":
                    _activeFilter = new FIRStrategy(_SampleRate, TargetFrequency, FIRbw /* BW */, 511 /* Taps */);
                    _activeFilter.ScalingFactor = _InputScalingFactor;
                    break;
                case "S/N":
                    _activeFilter = new FIRSNStrategy(_SampleRate, TargetFrequency, FIRbw /* BW */, 511 /* Taps */);
                    _activeFilter.ScalingFactor = _InputScalingFactor;
                    break;
                case "Goertzel":
                    _activeFilter = new GoertzelStrategy(TargetFrequency, _SampleRate, 256 /* blocksize */);
                    _activeFilter.ScalingFactor = _InputScalingFactor;
                    break;
                case "RMS":
                    _activeFilter = new RMSStrategy(_SampleRate, 256 /* blocksize */);
                    _activeFilter.ScalingFactor = _InputScalingFactor;
                    break;
                case "Hilbert Analytic":            //HBA
                    _activeFilter = new HilbertStrategy(127 /* taps */);
                    _activeFilter.ScalingFactor = _InputScalingFactor;
                    break;
            }
        }
        Log.Mark($"Filter changed to: {filterName}");
    }
}

//amplitude tracking and mark/space decision making
//With temporal drop out filter
public class AdaptiveThreshold
{
    private double _noiseFloor = 0.0;
    private double _peakLevel = 0.01;
    private double range = 0.01;
    private bool _isTone = false;
    private double spaceMeanSquare = 0;
    private double markMeanSquare = 0;
    private double spaceMeanSquareCount = 1;       //need to count accumulator to correctly scale power
    private double markMeanSquareCount = 1;         //  for SNratio calc

    public double SNratio { get; set; } = 0;

    // DEBOUNCE STATE
    private int _consecutiveHits = 0;           //init count
    private int _consecutiveMisses = 0;         //init count
    
    public double SquelchLevel { get; set; } = 0.01;        //set from UI
    public bool GaussianFilter = true;                      //diagnostic option
    public bool MedianFilter = true;                        //diagnostic option


    //Gaussian+median signal magnitude temporal filter version    
    private double[] _magHistory = new double[5]; // Gaussian window
    private double[] _medHistory = new double[3]; // Median window
    public bool IsSignalPresent(double rawMag)
    {
        double mag = rawMag;
        if (GaussianFilter)
        {
            // 1. GAUSSIAN SMOOTHING (5-Point)
            for (int i = 0; i < 4; i++) _magHistory[i] = _magHistory[i + 1];
            _magHistory[4] = rawMag;

            mag = (_magHistory[0] * 0.06) +
                    (_magHistory[1] * 0.24) +
                    (_magHistory[2] * 0.40) +
                    (_magHistory[3] * 0.24) +
                    (_magHistory[4] * 0.06);
        }

        if (MedianFilter)
        {
            // 2. MEDIAN FILTER (3-Point)
            // Shift median history
            _medHistory[0] = _medHistory[1];
            _medHistory[1] = _medHistory[2];
            _medHistory[2] = mag;

            // To find the median of 3 values, we sort them or use a simple swap-based check
            double a = _medHistory[0];
            double b = _medHistory[1];
            double c = _medHistory[2];

            // Simple median of 3
            mag = Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }

        // 3. DYNAMIC TRACKING (Peak and Noise)
        // 3.1. ALWAYS track noise floor so we know the environment
        _noiseFloor = (_noiseFloor * 0.995) + (mag * 0.005);

        // 3.2. SQUELCH AS A GATE
        bool isAboveSquelch = mag > SquelchLevel;

        if (isAboveSquelch)
        {
            // Only allow the Peak to move up or stay high if we are above Squelch
            if (mag > _peakLevel) _peakLevel = mag;
            else _peakLevel = (_peakLevel * 0.98) + (mag * 0.02);
        }
        else
        {
            // If below squelch, force the peak to decay toward the noise floor
            // This prevents the "Deafness" problem if a loud signal disappears
            _peakLevel = (_peakLevel * 0.95) + (_noiseFloor * 0.05);
        }

        // 3.3. CALCULATE DYNAMIC RANGE
        range = _peakLevel - _noiseFloor;

        // 4. THRESHOLDS & DEBOUNCE
        // SCHMITT TRIGGER (HYSTERESIS)
        // Baseline thresholds based on current noise and peak range
        double upperThreshold = _noiseFloor + (range * Config.THRESHOLD_UPPER_RATIO); // The "Engage" level
        double lowerThreshold = _noiseFloor + (range * Config.THRESHOLD_LOWER_RATIO); // The "Hold" level (lowered slightly for better QSB)

        bool meetsThreshold;
        if (!_isTone)
        {
            // If SILENT: Must cross the high bar to start a Mark
            meetsThreshold = (mag > upperThreshold);
        }
        else
        {
            // If TONING: Only need to stay above the low bar to continue the Mark
            meetsThreshold = (mag > lowerThreshold);
        }

        /* Single threashold solution
        double upperThreshold = _noiseFloor + (range * 0.50);
        double lowerThreshold = _noiseFloor + (range * 0.20);

        bool meetsThreshold = _isTone ? (mag > lowerThreshold) : (mag > upperThreshold);
        */

        if (meetsThreshold && mag > SquelchLevel)
        {
            _consecutiveHits++;
            _consecutiveMisses = 0;
            if (_consecutiveHits >= Config.MaxConsecutiveHits) _isTone = true;
        }
        else
        {
            _consecutiveMisses++;
            _consecutiveHits = 0;
            if (_consecutiveMisses >= Config.MaxConsecutiveMisses) _isTone = false;
        }

        //track S/N
        if (markMeanSquare == 0) markMeanSquare = Math.Pow(mag, 2);
        if (spaceMeanSquare == 0) spaceMeanSquare = Math.Pow(mag, 2);
        if (true)    //mag > SquelchLevel)
        {
            if (_isTone)
            {
                if (markMeanSquareCount <= spaceMeanSquareCount)
                {
                    markMeanSquare = markMeanSquare * 0.99 + Math.Pow(mag, 2) * 0.01;
                    markMeanSquareCount++;
                }
            }
            else
            {
                if (markMeanSquareCount > spaceMeanSquare)
                {
                    spaceMeanSquare = spaceMeanSquare * 0.99 + Math.Pow(mag, 2) * 0.01;
                    spaceMeanSquareCount++;
                }
            }
            if (markMeanSquareCount == spaceMeanSquareCount)
            {
                markMeanSquareCount = spaceMeanSquareCount = 1;     //avoid overrun on counter
            }
            // S/N not (S+N)/N
            double MarkEnergy = ((markMeanSquare - spaceMeanSquare) / markMeanSquareCount) / 2;      // 2 is window correction factor for tone
            SNratio = 100;
            if (spaceMeanSquare > 0.0000001)
            {
                SNratio = 10 * Math.Log10(MarkEnergy / (spaceMeanSquare / spaceMeanSquareCount));
            }
        }

        return _isTone;
    }



    // Helpers for CSV Logger
    public double GetCurrentThreshold() => _noiseFloor + (_peakLevel - _noiseFloor) * (_isTone ? 0.25 : 0.60);
    public double GetPeak() => _peakLevel;
    public double GetNoise() => _noiseFloor;
}
