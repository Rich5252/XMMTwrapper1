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
// - earlier FIR gave 98.7% accuracy scored using Levenshtein Benchmark Metric cf 88.6% using Fldigi
// - new FIR/Hilbert hybrid has 100% success rate at 6dB S/N on PARIS test (48kFs, 512 taps, 45Hz selected BW)
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

    private double _FIRbw = 45.0;
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
    public double GetSquelchLevel(out double level) => level = _adaptor.SquelchLevel;
    public void SetAutoSquelch(bool bAuto) => _adaptor.AutoSquelch = bAuto;
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

    private char lastChar = '\0';
    public CWDecoderEngine()
    {
        Task.Factory.StartNew(() => ProcessAudioLoop(), TaskCreationOptions.LongRunning);
        _clock.OnCharacterDecoded = (c) => { lastChar = c; OnCharacterDecoded?.Invoke(c); };
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
            BufferMilliseconds = 50
        };

        _SampleRate = _waveIn.WaveFormat.SampleRate;
        _clock.SetSampleRate(_SampleRate);

        //_activeFilter = new GoertzelStrategy(TargetFrequency, _SampleRate, 256 /* buffersize */);    //default filter
        _activeFilter = new FIRStrategy(_SampleRate, TargetFrequency, FIRbw /* BW */);    //default filter
        _activeFilter.ScalingFactor = _InputScalingFactor;


        _waveIn.DataAvailable += (s, e) => {
            if (!_audioQueue.IsAddingCompleted)
            {
                lock (_filterLock)
                {
                    byte[] data = new byte[e.BytesRecorded];
                    Array.Copy(e.Buffer, data, e.BytesRecorded);
                    _audioQueue.Add(data);
                }
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
    double magSmoothed = 0;                 //exp avg of magnitude for UI
    double SNratio= 0;

    double magFiltered = 0;
    double confFiltered = 0;

    //OLD - 50% overlap of incoming audio stream version
    //OLD - each 256 block is processed every 128 samples at 48kHz Fs (2.666ms per block)
    //OLD - although final code with FIR/Hilbert hybrid does not need overlap and uses 128 samples/block

    //NEW ********* Decimation  ******************
    // Because audio stream is pre-filtered by SDR we can decimate directly without additional pre-filter.
    // We should stay with magnitude output (input to CW Clock) every 2.666ms for timing consistency.
    // So if we decimate by 4, giving 12kHz sample rate, then we need to process every 32 samples (2.666ms).
    // The filters are being fed continuous data and already buffer incoming smaller sample blocks as required by # taps.
    // The big benefit is that the ratio between Fs and taps reduce with a corresponding reduction in bandwidth.
    // We can also increase FIR taps to give even better selectivity with no CPU cost penalty.
    //  (e.g. set 45Hz BW -> 512 taps at 48kHz = 327Hz ENBW, but 1024 taps at 12kHz = 70Hz ENBBW giving 7dB S/N improvement).
    //  ( 512 taps at 12kHz = 90Hz ENBW still much better than 48kHz case -> 6dB better)
    // However we wait and see if the narrow filters cause too much smear & ringing on fast code elements.
    // There will be some added latency due to lower Fs and more taps but that should not be a big issue for CW.
    //
    // For later consider further decimation and an FFT based filter bank approach.
    // A 128 point FFT at 6kHz Fs gives 47Hz bins, so we can have a bank of overlapping filters covering the CW band.
    // Gemini reports at 6kFs FIR 512 taps gives 41Hz ENB (6.1 MIPS) cf 256 FFT gives 40.5Hz ENB (3.8MIPS) both 2.66ms o/p.
    //   (needs checking)
    //NEW *****************************************

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
                    (isTone, magFiltered, confFiltered) = _adaptor.IsSignalPresent(mag, confidence);
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
                    _csvLogger.WriteLine($"{millis},{_clock.GetTotalSamples()},{magFiltered:F6},{_adaptor.GetCurrentThreshold():F6},{isTone},{(isTone ? 1 : 0)},{confFiltered:F4}, {lastChar}");
                    //_csvLogger.WriteLine($"{_clock.GetTotalSamples()},{magFiltered:F6},{_adaptor.GetCurrentThreshold():F6},{isTone},{(isTone ? 1 : 0)}");
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
                _csvLogger.WriteLine("Millis,#samples,Magnitude,Threshold,IsToning,State,Confidence, lastChar"); 
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
    private double spaceMeanSquare = 0;     //for S/N display
    private double markMeanSquare = 0;
    private double markRMSsquelch = 0;      //for auto squelch tracking
    private double spaceRMSsquelch = 0; 
    private int MarkSpaceRatio = 0;        //for auto squelch reset

    public double SNratio { get; set; } = 0;

    // DEBOUNCE STATE
    private int _consecutiveHits = 0;           //init count
    private int _consecutiveMisses = 0;         //init count
    
    public double SquelchLevel { get; set; } = 0.01;        //set from UI
    public bool AutoSquelch { get; set; } = false; 
    
    public bool GaussianFilter = true;                      //diagnostic option
    public bool MedianFilter = true;                        //diagnostic option 

    //Gaussian+median signal magnitude temporal filter version    
    private double[] _magGaussHistory = new double[5]; // Gaussian window
    private double[] _magMedHistory = new double[3]; // Median window
    private double[] _confGaussHistory = new double[5]; // Gaussian window
    private double[] _confMedHistory = new double[3]; // Median window



    //FIR + RMS + Goertzel all use this main logic
    public (bool, double, double) IsSignalPresent(double rawMag, double confidence = 1)
    {
        double mag = ApplyTemporalFilters(rawMag, 5, _magGaussHistory, _magMedHistory);       //5 point gaussian and median filtering
        double conf = ApplyTemporalFilters(confidence, 3, _confGaussHistory, _confMedHistory); //confidence filtering
        
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
        double signalPower = (mag * mag) / upperThreshold;
        if (!_isTone)
        {
            // If SILENT: Must cross the high bar to start a Mark
            double confidenceAdjust = (conf > 0.98 ? 1.4 : 1);
            //meetsThreshold = (mag * confidenceAdjust > upperThreshold);

            meetsThreshold = signalPower * confidenceAdjust > upperThreshold;
        }
        else
        {
            // If TONING: Only need to stay above the low bar to continue the Mark
            // confidence adjust to avoid drop outs
            //confidenceAdjust = (conf > 0.92  ? 1.4 : 1);
            //double confidenceAdjust = 1.0 + (Math.Max(0, conf - 0.5) * 1.0);
            //meetsThreshold = (mag * confidenceAdjust > lowerThreshold);

            meetsThreshold = signalPower > (lowerThreshold * 0.7);
        }

        if (meetsThreshold && mag > SquelchLevel)    /// && conf > Config.MinConfidence)
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

        if (meetsThreshold && mag > SquelchLevel && conf > Config.MinConfidence)
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
        /*        if (markMeanSquare == 0) markMeanSquare = Math.Pow(mag, 2);
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
        */
        
        if (_isTone)
        {
            markMeanSquare = markMeanSquare * 0.999 + Math.Pow(mag, 2) * 0.001;
            markRMSsquelch = markRMSsquelch * Config.SquelchSignalAvgFactor + mag * (1 - Config.SquelchSignalAvgFactor);
            MarkSpaceRatio++;
        }
        else
        {
            spaceMeanSquare = spaceMeanSquare * 0.999 + Math.Pow(mag, 2) * 0.001;
            spaceRMSsquelch = spaceRMSsquelch * Config.SquelchNoiseAvgFactor + mag * (1 - Config.SquelchNoiseAvgFactor);
            MarkSpaceRatio--;
        }

        if (MarkSpaceRatio < -1000) { MarkSpaceRatio = 0; markRMSsquelch = spaceRMSsquelch; }    //reset if no tone for long time
        if (AutoSquelch)
            SquelchLevel = spaceRMSsquelch + (markRMSsquelch - spaceRMSsquelch) * Config.AutoSquelchLevelFactor;   //set squelch 30%
        

        // S/N not (S+N)/N
        double MarkEnergy = (markMeanSquare - spaceMeanSquare); // / 2;      // 2 is window correction factor for tone

        SNratio = 100;       
        if (MarkEnergy > 0.00000001 && spaceMeanSquare > 0.00000001)
        {
            SNratio = 10 * Math.Log10(MarkEnergy / (spaceMeanSquare));      // / spaceMeanSquareCount));
        }

        return (_isTone, mag, conf);
    }

    //initial logic
    public bool IsHBASignalPresentA(double rawMag, double confidence)
    {
        // 1. Keep your 3-point Gaussian/Median smoothing here
        double smoothedMag = ApplyTemporalFilters(rawMag, 3, _magGaussHistory, _magMedHistory);

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
        double smoothedMag = ApplyTemporalFilters(mag, 3, _magGaussHistory, _magMedHistory);
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
        double smoothedMag = ApplyTemporalFilters(mag, 3, _magGaussHistory, _magMedHistory);

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

    private double ApplyTemporalFilters(double rawMag, int nPoint, double[] gHistory, double[] mHistory){
        // 1. FILTERING (Keep the filters, but maybe reduce Gaussian to 3-point if lag is high)
        double mag = rawMag;
        if (GaussianFilter && nPoint == 3)
        {
            // 1. GAUSSIAN SMOOTHING (3-Point)
            gHistory[0] = gHistory[1];
            gHistory[1] = gHistory[2];
            gHistory[2] = rawMag;

            // Apply Gaussian weights: 1/4, 1/2, 1/4
            mag = (gHistory[0]* 0.25) +
                  (gHistory[1]* 0.50) +
                  (gHistory[2]* 0.25);
        }

        if (GaussianFilter && nPoint == 5)
        {
            // 1. GAUSSIAN SMOOTHING (5-Point)
            for (int i = 0; i < 4; i++) gHistory[i] = gHistory[i + 1];
            gHistory[4] = rawMag;

            mag = (gHistory[0] * 0.06) +
                    (gHistory[1] * 0.24) +
                    (gHistory[2] * 0.40) +
                    (gHistory[3] * 0.24) +
                    (gHistory[4] * 0.06);
        }

        if (MedianFilter)
        {
            // 2. MEDIAN FILTER (3-Point)
            // Shift median history
            mHistory[0] = mHistory[1];
            mHistory[1] = mHistory[2];
            mHistory[2] = mag;

            // To find the median of 3 values, we sort them or use a simple swap-based check
            double a = mHistory[0];
            double b = mHistory[1];
            double c = mHistory[2];

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
