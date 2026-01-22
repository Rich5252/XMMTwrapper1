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

using Microsoft.VisualStudio.OLE.Interop;
using NAudio.Wave;                          //audio support library
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Text;

public class CWDecoderEngine
{
    private bool _isRunning = false;
    private readonly BlockingCollection<byte[]> _audioQueue = new BlockingCollection<byte[]>();
    private List<float> _residualBuffer = new List<float>();
    private WaveInEvent _waveIn;
    private AdaptiveThreshold _adaptor = new AdaptiveThreshold();
    private MorseClock _clock = new MorseClock();

    //setup messaging to Form UI
    public Action<double, double> OnMagnitudeUpdated;
    public Action<double> OnSNratioUpdated;
    public Action<bool> OnToneDetected;
    public Action<string> OnSequenceChanged;
    public Action<char> OnCharacterDecoded;
    public Action<int> OnWpmChanged;

    //Params adjustable from UI
    private double _TargetFrequency = 1000.0;
    public double TargetFrequency {
        get => _TargetFrequency;
        set { _TargetFrequency = value; _activeFilter.TargetFrequency = _TargetFrequency; }
    }

    private double _FIRbw = 50.0;
    public double FIRbw {
        get => _FIRbw;
        set { _FIRbw = value; _activeFilter.filterBW = _FIRbw; }        //now filters adjustable on the fly
    }     

    private double _InputScalingFactor = 200;
    private double _HBAtol = 30;                        //HBA discrim tolerance (Hz)
    public void SetInputScale(double _SF)
    {
        _InputScalingFactor = _SF;
        _activeFilter.ScalingFactor = _InputScalingFactor;
    }

    public void SetTolerance(double _tol)
    {
        _HBAtol = _tol;
        _activeFilter.Tolerance = _HBAtol;
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
        _activeFilter = new FIRStrategy(_SampleRate, TargetFrequency, FIRbw /* BW */);    //default filter
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
        int processBlockSize = 128;         //if size == step then no overlap
        int processBlockStep = 128;         //represents 50% overlap for 256 blocksize
        bool isTone = false;
        foreach (var buffer in _audioQueue.GetConsumingEnumerable())
        {
            float[] newSamples = ConvertToFloat(buffer);
            _residualBuffer.AddRange(newSamples);

            // Slide window by 128 samples (50% overlap of a 256 block)
            while (_residualBuffer.Count >= processBlockSize)
            {
                // 1. Get the current 256 block
                float[] block = _residualBuffer.GetRange(0, processBlockSize).ToArray();

                // 2. SLIDE: Only remove 128 samples, not 256 if 50% overlap wanted
                processBlockStep = (_activeFilter.isHilbert() ? processBlockSize : 128);    //no overlap for Hilbert Analytic
                _residualBuffer.RemoveRange(0, processBlockStep);

                // 3. Process
                // Unified call regardless of strategy
                double mag = _activeFilter.ProcessBlock(block);         //filter and return scaler magnitude
                double confidence = _activeFilter.SigConfidence();      //Hibert phase lock confidence (0.0 to 1.0)
                if (!_activeFilter.isHilbert())
                {
                    isTone = _adaptor.IsSignalPresent(mag, confidence);
                }
                else
                {
                    isTone = _adaptor.IsHBASignalPresent(mag, confidence);
                }
                
                SNratio = _adaptor.SNratio;                             //pick up the current SN measurement for UI

                
                // Inside ProcessAudioLoop, after IsSignalPresent(mag):
                if (_csvLogger != null)
                {
                    long millis = sw.ElapsedMilliseconds;
                    // Record the decision-making data
                    _csvLogger.WriteLine($"{millis},{_clock.GetTotalSamples()},{mag:F6},{_adaptor.GetCurrentThreshold():F6},{isTone},{(isTone ? 1 : 0)},{confidence:F4}");
                    //_csvLogger.WriteLine($"{_clock.GetTotalSamples()},{mag:F6},{_adaptor.GetCurrentThreshold():F6},{isTone},{(isTone ? 1 : 0)}");
                }

                if (magSmoothed == 0) { magSmoothed = mag; }
                else { magSmoothed = magSmoothed * 0.99 + mag * 0.01; }
                _updateCounter++;
                if (_updateCounter >= 50)
                {
                    OnMagnitudeUpdated?.Invoke(magSmoothed, confidence);
                    OnToneDetected?.Invoke(isTone);
                    OnSNratioUpdated?.Invoke(SNratio);
                    _updateCounter = 0;
                }

                // 4. Update Clock with given samples of "time" for accurate timing
                // This also allows overlap processing to work correctly
                _clock.UpdateState(isTone, processBlockStep);
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

    public void SetWpm(int wpm) => _clock.SetWpm(wpm);
    public bool FixWpm(bool fix) => _clock.FixWpm(fix);

    // Diagnostic logging. Call from UI
    private readonly object _logLock = new object();
    private StreamWriter _csvLogger;
    Stopwatch sw;
    public void StartLogging()
    {
        EnableLogging(LoggingFilePath);
    }
    public void EnableLogging(string filePath)
    {
        lock (_logLock)
        {
            sw = Stopwatch.StartNew();
            try
            {
                _csvLogger = new StreamWriter(filePath);
                _csvLogger.WriteLine("Millis,#samples,Magnitude,Threshold,IsToning,State,Confidence"); 
            }
            catch 
            {
                MessageBox.Show("Error creating log file. Check if file is already open");
            } 
        }
    
    }
    public void StopLogging()
    {
        lock (_logLock)
        {
            if (_csvLogger != null)
            {
                sw.Stop();
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
                    _activeFilter = new FIRStrategy(_SampleRate, TargetFrequency, FIRbw /* BW */);
                    _activeFilter.ScalingFactor = _InputScalingFactor;
                    break;
                case "Goertzel":
                    _activeFilter = new GoertzelStrategy(_SampleRate, TargetFrequency, 256 /* blocksize */);
                    _activeFilter.ScalingFactor = _InputScalingFactor;
                    break;
                case "RMS":
                    _activeFilter = new RMSStrategy(_SampleRate, 256 /* blocksize */);
                    _activeFilter.ScalingFactor = _InputScalingFactor;
                    break;
                case "Hilbert Analytic":            //HBA
                    _activeFilter = new HilbertStrategy( _SampleRate, TargetFrequency, FIRbw /* BW */);
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

    //FIR + RMS + Goertzel all use this main logic
    public bool IsSignalPresent(double rawMag, double confidence = 1)
    {
        double mag = ApplyTemporalFilters(rawMag, 5);       //5 point gaussian and median filtering

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

        //Ready to incorporate confidence weighting here if needed
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

        if (meetsThreshold && mag > SquelchLevel)    // && confidence > Config.MinConfidence)
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

        /* //FIR ONLY version
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

        if (meetsThreshold && mag > SquelchLevel && confidence > Config.MinConfidence)
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
        */

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

    //initial logic
    public bool IsHBASignalPresentA(double rawMag, double confidence)
    {
        // 1. Keep your 3-point Gaussian/Median smoothing here
        double smoothedMag = ApplyTemporalFilters(rawMag, 3);

        // 2. INTELLIGENT TRACKING
        if (confidence > Config.MinConfidence && smoothedMag > SquelchLevel)
        {
            // High confidence signal: Update Peak
            if (smoothedMag > _peakLevel) _peakLevel = smoothedMag;
            else _peakLevel = (_peakLevel * 0.99) + (smoothedMag * 0.01);
        }
        else if (!_isTone)
        {
            // Low confidence + No active tone: Update Noise Floor
            _noiseFloor = (_noiseFloor * 0.99) + (smoothedMag * 0.01);

            // Decay peak slowly toward noise to stay adaptive
            _peakLevel = (_peakLevel * 0.995) + (_noiseFloor * 0.005);
        }

        range = Math.Max(0.001, _peakLevel - _noiseFloor);

        // 3. SCHMITT TRIGGER WITH CONFIDENCE
        double upperThreshold = _noiseFloor + (range * Config.THRESHOLD_UPPER_RATIO);
        double lowerThreshold = _noiseFloor + (range * Config.THRESHOLD_LOWER_RATIO);

        // Meets criteria if Magnitude is right AND we have a phase-lock
        bool looksLikeTone = (confidence > Config.MinConfidence) &&
                             (_isTone ? smoothedMag > lowerThreshold : smoothedMag > upperThreshold);

        // 4. DEBOUNCE      
        if (looksLikeTone && smoothedMag > SquelchLevel)
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
        

        return _isTone;
    }

    //after "P" log analysis, add mag * confidence to improve end of tone detection
    public bool IsHBASignalPresent(double mag, double confidence)
    {
        double smoothedMag = ApplyTemporalFilters(mag, 3);
        double phaseWeightedMag = smoothedMag * confidence;

        // 1. DYNAMIC TRACKING (Strict update)
        // Inside IsSignalPresentB
        if (confidence > 0.92 && smoothedMag > SquelchLevel)
        {
            // Update Peak while toning
            if (smoothedMag > _peakLevel) _peakLevel = smoothedMag;
            else _peakLevel = (_peakLevel * 0.98) + (smoothedMag * 0.02);
        }
        else if (!_isTone)
        {
            // Silent: Force the Peak down towards the Noise Floor
            // This is what prevents the 0.73 overshoot in your log.
            _noiseFloor = (_noiseFloor * 0.99) + (smoothedMag * 0.01);
            _peakLevel = (_peakLevel * 0.85) + (_noiseFloor * 0.15);
        }

        // Calculate Range with a safety floor
        double currentRange = Math.Max(0.1, _peakLevel - _noiseFloor);

        // HYSTERESIS TUNING
        // While TONING: Keep the bar low (25%) to hold the Dah
        double lowerThreshold = _noiseFloor + (currentRange * 0.25);

        // While SILENT: Use a 40% bar instead of 50-60%
        // This prevents the '0.69' jump you saw.
        double upperThreshold = _noiseFloor + (currentRange * 0.38);

        // EXTRA SAFETY: Clamp the Upper Threshold
        // Never let the 'start' bar be higher than, say, 0.5 if we are in a quiet patch.
        upperThreshold = Math.Min(upperThreshold, 0.55);

        // 3. LOGIC
        bool meetsThreshold = _isTone ? (smoothedMag > lowerThreshold) : (smoothedMag > upperThreshold);

        // REQUIRE high confidence to START, but only moderate to HOLD
        bool confidenceGate = _isTone ? (confidence > 0.45) : (confidence > 0.90);

        if (meetsThreshold && confidenceGate && smoothedMag > SquelchLevel)
        {
            _consecutiveHits++;
            _consecutiveMisses = 0;
            if (_consecutiveHits >= 2) _isTone = true;
        }
        else
        {
            _consecutiveMisses++;
            _consecutiveHits = 0;

            // Use the mag*conf product for the fast stop
            if (phaseWeightedMag < lowerThreshold || confidence < 0.3)
            {
                if (_consecutiveMisses >= 2) _isTone = false;
            }
            else
            {
                // Hang-time to prevent splitting Dahs during minor fades
                if (_consecutiveMisses >= 8) _isTone = false;
            }
        }

        return _isTone;
    }

    //pretty good results with 70Hz BW HBA filter + 50Hz tol bw
    public bool IsHBASignalPresentB(double mag, double confidence)
    {
        // 1. Keep your 3-point Gaussian/Median smoothing here
        double smoothedMag = ApplyTemporalFilters(mag, 3);

        // 2. ONLY update tracking if we have high confidence
        // This prevents the Threshold from 'spiking' due to noise
        if (confidence > 0.9 && smoothedMag > SquelchLevel)
        {
            if (smoothedMag > _peakLevel) _peakLevel = smoothedMag;
            else _peakLevel = (_peakLevel * 0.98) + (smoothedMag * 0.02);
        }
        else if (!_isTone)
        {
            // Only update noise floor when we are SURE there is no tone
            _noiseFloor = (_noiseFloor * 0.99) + (smoothedMag * 0.01);
        }

        // 3. Dynamic Range
        double currentRange = Math.Max(0.01, _peakLevel - _noiseFloor);
        double upperThreshold = _noiseFloor + (currentRange * Config.THRESHOLD_UPPER_RATIO);
        double lowerThreshold = _noiseFloor + (currentRange * Config.THRESHOLD_LOWER_RATIO);

        // 4. THE DEBOUNCE (The Core)
        // We require phase lock to START a tone, but we are more lenient to CONTINUE it
        bool meetsCriteria = _isTone
            ? (smoothedMag > lowerThreshold && (confidence > 0.45)) // Already toning? Just stay above the bar.
            : (smoothedMag > upperThreshold && confidence > 0.85); // Starting? Need Phase Lock.

        if (meetsCriteria)
        {
            _consecutiveHits++;
            _consecutiveMisses = 0;
            if (_consecutiveHits >= 2) _isTone = true; // Fast attack (2 blocks ~10ms)
        }
        else
        {
            _consecutiveMisses++;
            _consecutiveHits = 0;
            // Moderate decay (3 blocks ~15ms) to prevent chatter
            if (_consecutiveMisses >= 5) _isTone = false;       //was 3
        }

        return _isTone;
    }

    private double ApplyTemporalFilters(double rawMag, int nPoint = 3){
        // 1. FILTERING (Keep the filters, but maybe reduce Gaussian to 3-point if lag is high)
        double mag = rawMag;
        if (GaussianFilter && nPoint == 3)
        {
            // 1. GAUSSIAN SMOOTHING (3-Point)
            _magHistory[0] = _magHistory[1];
            _magHistory[1] = _magHistory[2];
            _magHistory[2] = rawMag;

            // Apply Gaussian weights: 1/4, 1/2, 1/4
            mag = (_magHistory[0]* 0.25) +
                  (_magHistory[1]* 0.50) +
                  (_magHistory[2]* 0.25);
        }

        if (GaussianFilter && nPoint == 5)
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
        return mag;
    }

    
    // Helpers for CSV Logger
    public double GetCurrentThreshold() => _noiseFloor + (_peakLevel - _noiseFloor) * (_isTone ? Config.THRESHOLD_LOWER_RATIO : Config.THRESHOLD_UPPER_RATIO);
    public double GetPeak() => _peakLevel;
    public double GetNoise() => _noiseFloor;
}
