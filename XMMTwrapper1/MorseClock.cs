// ***************************************************
//Strategy Header: Statistical Rhythm Adaptive Decoder
// ***************************************************
//Core Logic: This engine utilizes a Dual-Bucket Median Tracking strategy to determine Morse speed.
//Unlike decoders that only track the length of "dots," this engine tracks both Mark Dots and Element
//Spaces independently. By calculating the median of these two buckets and averaging them, the engine
//derives a "True Dot Unit" that is resistant to "weighting" (where a radio or keyer makes beeps
//longer than the silences between them).

//Key Parameter Definitions (see MorseTuningConfig.cs for latest values):

//Mark Classification(DASH_THRESHOLD_RATIO = 2.5):
//  Sets the boundary between a dot and a dash. Using 2.5 (instead of the theoretical 2.0) provides
//  a "safety buffer" to prevent noise-stretched dots from being misidentified as dashes.

//Space Classification (LETTER_SPACE_MIN_RATIO = 2.4):
//  The threshold to trigger a character decode. When silence exceeds 2.4x the current dot unit,
//  the current buffer is sent to the lookup table.

//Symmetrical Compensation (FILTER_COMPENSATION_MS = 2):
//  A fixed time-bias added to detected durations to counteract the "smearing" effect of the
//  FIR filter skirts, which tends to lengthen marks and shorten spaces.

//Adaptive Debounce (DEBOUNCE_RATIO = 0.25):
//  A dynamic gate that ignores state changes shorter than 25% of a dot. This effectively ignores
//  high-frequency "chatter" or transients without affecting 30-40 WPM timing.

//Weighted Adaptation (ADAPTATION_BASE_WEIGHT = 0.04):
//  Controls the "inertia" of the WPM tracker. It uses a Standard Deviation check to determine "Confidence";
//  if the incoming rhythm is consistent, it allows the tracker to adapt faster.

//Speed Stability Strategy: The engine maintains a history of the last 30 elements (Buckets).
//  By using the Median rather than the Mean, the engine inherently ignores "outliers" such as spurious
//  noise spikes (extra-short dots) or signal fades (extra-long elements) that would otherwise cause "WPM bounce."

// ***************************************************************************************************************

using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics;
using System.Text;


public class MorseClock
{
    private double _sampleRate = 48000.0;
    private long _totalSamplesProcessed = 0;
    private long _lastTransitionSample = 0;
    private bool _lastToneState = false;
    private long _pendingMarkDuration = 0; // New class-level variable
    private StringBuilder _currentSymbolBuffer = new StringBuilder();

    private double _dotMs = 1200.0 / 25.0;
    private double _currentWPM = 25;
    private bool _FixWpm = false;

    // --- TRACKING BUCKETS ---
    private List<double> _dotHistory = new();
    private List<double> _dashHistory = new();
    private List<double> _spaceHistory = new();

    public Action<string> OnSequenceChanged;
    public Action<char> OnCharacterDecoded;
    public Action<int> OnWpmChanged;

    private readonly object _lock = new object();

    public void SetSampleRate(int rate) => _sampleRate = rate;
    
    public long GetTotalSamples() => _totalSamplesProcessed;

    public bool FixWpm(bool fix) => _FixWpm = fix;
    public void SetWpm(int Wpm)
    {
        lock (_lock)
        {
            _dotMs = 1200 / Wpm;
            _currentWPM = Wpm;
            SeedDotDashHistory(_currentWPM);        //Initialize history to force reset
        }
    }

    private int stateCounter = 0;
    private long _noiseCarryoverMs = 0;
    public void UpdateState(bool isTonePresent, int samplesInBlock)
    {
        lock (_lock)
        {
            _totalSamplesProcessed += samplesInBlock;

            // 1. Idle/Same State Management
            if (isTonePresent == _lastToneState)
            {
                if (!isTonePresent && _currentSymbolBuffer.Length > 0)
                {
                    // Include carryover in the silence calculation
                    double silenceMs = GetElapsedMs() + _noiseCarryoverMs;

                    if (_pendingMarkDuration > 0 && silenceMs > _dotMs * 0.5)
                    {
                        // If we timeout while holding a tiny mark, it was noise
                        _noiseCarryoverMs += (long)_pendingMarkDuration;
                        _pendingMarkDuration = 0;
                    }

                    if (silenceMs > _dotMs * Config.IDLE_DECODE_RATIO)
                    {
                        _noiseCarryoverMs = 0; // Clear carryover on decode
                        DecodeBuffer();
                    }
                }
                return;
            }

            double elapsedMs = GetElapsedMs();

            // 3. Adaptive Debounce
            if (elapsedMs < Math.Max(Config.DOT_MINIMUM_MS, _dotMs * Config.DEBOUNCE_RATIO)) return;

            // 4. ASYMMETRICAL COMPENSATION
            long adjusted;
            if (_lastToneState) // Falling Edge
                adjusted = (long)elapsedMs - Config.FILTER_COMPENSATION_MS;
            else // Rising Edge
                adjusted = (long)elapsedMs + Config.FILTER_COMPENSATION_MS;

            // 5. LOOK-FORWARD LOGIC (The "Noise vs. Element" Filter)
            if (_lastToneState && !isTonePresent) // Falling Edge (End of Mark)
            {
                // A mark is "Suspicious" only if it's extremely short AND we have a stable history.
                // If it's > 50% of a dot, it's almost certainly a real 'E' or part of a character.
                if (_dotHistory.Count < 5 || adjusted >= Config.DOT_MINIMUM_MS)     //_dotMs * 0.5)
                {
                    ProcessMark(adjusted);
                    _pendingMarkDuration = 0;
                }
                else
                {
                    // This is a "Blip." Hold it to see what the following space looks like.
                    _pendingMarkDuration = adjusted;
                }
            }
            else if (!_lastToneState && isTonePresent) // Rising Edge (End of Space)
            {
                if (_pendingMarkDuration > 0)
                {
                    // The "Trap": If the space after a tiny blip is long (>80% of a dot), 
                    // the blip was noise. We add the blip AND this space to the PREVIOUS space.
                    if (adjusted > _dotMs * 0.8)
                    {
                        // We DON'T call ProcessSpace(adjusted). 
                        // We tell the system: "That noise spike didn't happen, just keep the space going."
                        // This is done by calling ProcessSpace with the cumulative total.
                        ProcessSpace(adjusted + _pendingMarkDuration, isContinuation: true);
                        Log.Perf($"NOISE_TRAP: Merged {adjusted + _pendingMarkDuration}ms into existing silence.");
                    }
                    else
                    {
                        // The space was tight! The blip was a real (short) dot.
                        ProcessMark(_pendingMarkDuration);
                        ProcessSpace(adjusted);
                    }
                    _pendingMarkDuration = 0;
                }
                else
                {
                    ProcessSpace(adjusted);
                }
            }

            _lastToneState = isTonePresent;
            _lastTransitionSample = _totalSamplesProcessed;
        }
    }

    /*
    public void UpdateState(bool isTonePresent, int samplesInBlock)
    {
        lock (_lock)
        {
            _totalSamplesProcessed += samplesInBlock;

            // 1. Idle/Same State Management
            if (isTonePresent == _lastToneState)
            {
                if (!isTonePresent && _currentSymbolBuffer.Length > 0)
                {
                    double silenceMs = GetElapsedMs();
                    // If we have a pending noise spike and hit idle, discard it
                    if (_pendingMarkDuration > 0 && silenceMs > _dotMs * 0.5)
                    {
                        _pendingMarkDuration = 0;
                    }
                    if (silenceMs > _dotMs * Config.IDLE_DECODE_RATIO) DecodeBuffer();
                }
                return;
            }

            // 2. Measure transition
            double elapsedMs = GetElapsedMs();

            // 3. Adaptive Debounce
            if (elapsedMs < _dotMs * Config.DEBOUNCE_RATIO) return;

            //ignore very small durations to avoid noise
            //if your dotMs * DEBOUNCE_RATIO gets too small.
            if (elapsedMs < Math.Max(Config.DOT_MINIMUM_MS, _dotMs * Config.DEBOUNCE_RATIO)) return;

            // 4. Compensation
            //long adjusted = (long)elapsedMs + Config.FILTER_COMPENSATION_MS;

            // 4. ASYMMETRICAL COMPENSATION
            // Subtract from Marks, Add to Spaces to recover the "True" duration.
            long adjusted;
            if (_lastToneState) // We just finished a TONE (Falling Edge)
            {
                // -7ms is your identified "sweet spot"
                adjusted = (long)elapsedMs - Config.FILTER_COMPENSATION_MS;         //Config.MARK_OFFSET_MS;
            }
            else // We just finished a SPACE (Rising Edge)
            {
                // Adding 7ms back to the space keeps the total Cadence (Dot+Gap) correct.
                adjusted = (long)elapsedMs + Config.FILTER_COMPENSATION_MS;           //Config.MARK_OFFSET_MS;
            }

            // 5. LOOK-FORWARD LOGIC (The "E" Filter)
            if (_lastToneState && !isTonePresent) // Falling Edge (End of Mark)
            {
                // If the mark is suspiciously short (e.g., < 50% of a dot), hold it
                // If we have no history, don't try to "Look Forward" — we don't know enough to filter yet.
                if (_dotHistory.Count < 5 || adjusted >= _dotMs * 0.5)
                {
                    //so if valid mark or no history yet, process immediately
                    ProcessMark(adjusted);
                    _pendingMarkDuration = 0;
                }
                else
                {
                    _pendingMarkDuration = adjusted;
                }
            }
            else if (!_lastToneState && isTonePresent) // Rising Edge (End of Space)
            {
                if (_pendingMarkDuration > 0)
                {
                    // LOOK FORWARD: We just finished a space that followed a tiny mark.
                    // If this space is also short, the "Mark" was likely a real (but fast) dot.
                    // If this space is long, the "Mark" was almost certainly a noise spike.
                    if (adjusted > _dotMs * 0.8)
                    {
                        // NOISE DETECTED: Discard the pending mark.
                        // Instead of calling ProcessSpace for just this gap, 
                        // we could optionally add the noise duration to the total space.
                        //LogSignalHealth(adjusted, false);                     
                        ProcessSpace(adjusted + _pendingMarkDuration);
                    }
                    else
                    {
                        // VALID DOT: It was short, but followed by a tight element space.
                        //LogSignalHealth(_pendingMarkDuration, true);
                        ProcessMark(_pendingMarkDuration);
                        ProcessSpace(adjusted);
                    }
                    _pendingMarkDuration = 0;
                }
                else
                {
                    ProcessSpace(adjusted);
                }
            }

            if (isTonePresent != _lastToneState)    //tone state changed
            {
                //log time in same state
                double sampleCount = (_totalSamplesProcessed - _lastTransitionSample);
                Log.Perf($"STATE_CHANGE: {_lastToneState} for {sampleCount} samples ({(sampleCount * 1000.0) / 48000.0} ms)");
            }

            _lastToneState = isTonePresent;
            _lastTransitionSample = _totalSamplesProcessed;
        }
    }
    */


    private void ProcessMark(long duration)
    {
        //LogSignalHealth(duration, true);

        // 1. Reset the space accumulator because the silence has ended
        _lastSpaceDuration = 0;

        // 1. DASH CLASSIFICATION
        bool isDash = duration > (_dotMs * Config.DASH_THRESHOLD_RATIO);

        // 2. MARK VALIDATION (The "E" Filter)
        // If it's too short to be a dot (e.g., < 50% of expected), it's noise.
        // If it's in the 'no-man's land' between dot and dash, it's suspect.
        bool isValidDot = !isDash && (duration > _dotMs * Config.MIN_DOT_WINDOW && duration < _dotMs * Config.MAX_DOT_WINDOW);
        bool isValidDash = isDash && (duration > _dotMs * Config.MIN_DASH_WINDOW && duration < _dotMs * Config.MAX_DASH_WINDOW);

        if (isDash)
        {
            if (isValidDash)
            {
                _dashHistory.Add(duration);
                if (_dashHistory.Count > Config.HISTORY_BUCKET_SIZE) _dashHistory.RemoveAt(0);
            }
        }
        else
        {
            // Only let "clean" dots update our speed tracking
            if (isValidDot)
            {
                _dotHistory.Add(duration);
                if (_dotHistory.Count > Config.HISTORY_BUCKET_SIZE) _dotHistory.RemoveAt(0);
            }
        }

        // 3. DECODE BUFFER
        // We still append the symbol to the buffer so the user sees the "hit", 
        // but the speed tracker is now protected from the jitter.
        _currentSymbolBuffer.Append(isDash ? "-" : ".");
        OnSequenceChanged?.Invoke(_currentSymbolBuffer.ToString());

        // Only update speed if the element was statistically "sane"
        if (isValidDot || isValidDash) UpdateSpeedTracking();
    }

    private bool isLastCharSpace = true;
    private long _lastSpaceDuration = 0;
    private void ProcessSpace(long duration, bool isContinuation = false)
    {
        //LogSignalHealth(duration, false);
        //Continuation means we are merging noise into existing space, so skip processing
        if (isContinuation)
        {
            // Add this "noise + gap" to the previous gap instead of starting a new one
            _lastSpaceDuration += duration;
        }
        else
        {
            _lastSpaceDuration = duration;
        }

        // Tier 1: Element Space (Internal Timing)
        if (_lastSpaceDuration < _dotMs * Config.LETTER_SPACE_MIN_RATIO)
        {
            _spaceHistory.Add((double)duration);
            if (_spaceHistory.Count > Config.HISTORY_BUCKET_SIZE) _spaceHistory.RemoveAt(0);
            UpdateSpeedTracking();
        }
        // Tier 2: Letter Space (Trigger Decode)
        else if (_lastSpaceDuration >= _dotMs * Config.LETTER_SPACE_MIN_RATIO && duration < _dotMs * Config.WORD_SPACE_MIN_RATIO)
        {
            if (DecodeBuffer()) isLastCharSpace = false;        //only reset if something was decoded
        }
        // Tier 3: Word Space
        else if (_lastSpaceDuration >= _dotMs * Config.WORD_SPACE_MIN_RATIO)
        {
            if (isLastCharSpace) return;    //avoid multiple spaces (cant be a word end if last char was space)
            DecodeBuffer();                 //word end on first space
            OnCharacterDecoded?.Invoke(' ');
            isLastCharSpace = true;
        }
    }

    private void LogSignalHealth(long duration, bool isMark)
    {
        double ratio = duration / _dotMs;

        // DASH DANGER ZONE: Was it almost a dot?
        if (isMark && ratio > 1.6 && ratio < 2.4)
        {
            Log.Perf($"[QSB MARK] Duration: {duration}ms (Ratio: {ratio:F2}) - Near Dash Threshold.");
        }

        // SPACE DANGER ZONE: Was it almost a character break?
        if (!isMark && ratio > 1.6 && ratio < 2.4)
        {
            Log.Perf($"[QSB SPACE] Duration: {duration}ms (Ratio: {ratio:F2}) - Near Letter Gap.");
        }
    }

    private void UpdateSpeedTracking()
    {
        if (_dotHistory.Count < 3) return;

        // 1. Use a Weighted Average instead of Median for faster "pivot"
        double currentAverage = 0;
        double weightSum = 0;
        for (int i = 0; i < _dotHistory.Count; i++)
        {
            // Recent dots are 3x more important than old ones
            double w = (i >= _dotHistory.Count - 5) ? 1.0 : 1.0;        //first param was 3 to inc weight on recent values
            currentAverage += _dotHistory[i] * w;
            weightSum += w;
        }
        double targetDotMs = currentAverage / weightSum;

        // 2. ASYMMETRIC GATE
        // We slow down (increase dotMs) faster than we speed up (decrease dotMs).
        double delta = targetDotMs - _dotMs;

        // If delta > 0, the station is SLOWER than our clock (Trust this more)
        // If delta < 0, the station is FASTER than our clock (Be skeptical, could be noise)
        double trackingRate = (delta > 0) ? 0.15 : 0.05; // 15% adjustment for slow-down, 5% for speed-up

        double maxChange = _dotMs * trackingRate;

        if (Math.Abs(delta) > maxChange)
        {
            _dotMs += Math.Sign(delta) * maxChange;
        }
        else
        {
            _dotMs = targetDotMs;
        }

        // 3. WPM Update & Sanity
        double newWpm = 1200.0 / _dotMs;

        // If we drift into impossible territory, hard reset to a "Contest Center" of 25 WPM
        if (newWpm > 42 || newWpm < 15)
        {
            SeedDotDashHistory(25.0);
            return;
        }

        if (!_FixWpm) _currentWPM = newWpm;
        OnWpmChanged?.Invoke((int)_currentWPM);
    }

    /*
    private void UpdateSpeedTracking()
    {
        if (_dotHistory.Count < 3 || _spaceHistory.Count < 3) return;

        // 1. Calculate the raw median from the current buckets
        double rawDotMedian = MorseMath.CalculateMedian(_dotHistory);

        // 2. APPLY AN ADAPTIVE GATE
        // Only allow the internal _dotMs to move by a small percentage per update.
        // This prevents a burst of noise (15ms dots) from "yanking" the WPM to 60.
        double maxChange = _dotMs * 0.05; // Limit change to 5% per update
        double delta = rawDotMedian - _dotMs;

        if (Math.Abs(delta) > maxChange)
        {
            _dotMs += Math.Sign(delta) * maxChange;
        }
        else
        {
            _dotMs = rawDotMedian;
        }

        double newWpm = 1200.0 / _dotMs;
        if (newWpm > 40 || newWpm < 15)
        {
            newWpm = 25.0;
            SeedDotDashHistory(newWpm);
        }
        if ((int)_currentWPM != (int)newWpm)
        {
            _currentWPM = newWpm;
            OnWpmChanged?.Invoke((int)_currentWPM);
        }
    }
    */
    
    private bool DecodeBuffer()
    {
        string sequence = _currentSymbolBuffer.ToString();
        if (string.IsNullOrEmpty(sequence)) return false;       //nothing to decode

        char result = MorseTable.Lookup(sequence);
        if (result != '\0') OnCharacterDecoded?.Invoke(result);

        _currentSymbolBuffer.Clear();
        OnSequenceChanged?.Invoke("");
        return true;                                //decoded
    }

    private double GetElapsedMs() => ((_totalSamplesProcessed - _lastTransitionSample) / _sampleRate) * 1000.0;

    private void SeedDotDashHistory(double Wpm)
    {
        lock (_lock)
        {
            double dotMs = 1200 / Wpm;

            // 1. Set the immediate working value so the FIRST element is classified correctly
            _dotMs = dotMs;

            // 2. Clear previous history
            _dotHistory.Clear();
            _dashHistory.Clear();
            _spaceHistory.Clear();

            // 3. "Light Seeding": Add only 3-5 elements
            // This provides a baseline but allows the engine to pivot to a new speed 
            // within just one or two characters.
            for (int i = 0; i < 5; i++)
            {
                _dotHistory.Add(dotMs);
                _dashHistory.Add(dotMs * 3.0);
                _spaceHistory.Add(dotMs * 1.0); // Element spaces are 1 unit!
            }
        }
    }

    /*
    private void SeedDotDashHistory(double Wpm)
    {
        //set or reset dot/dash history
        for (int i = 0; i < Config.HISTORY_BUCKET_SIZE; i++)
        {
            double dotMs = 1200 / Wpm;
            _dotHistory.Add(dotMs);
            _dashHistory.Add(dotMs * 3);
            _spaceHistory.Add(dotMs * 3);       ************** BUG should be * 1 dit/dah space
        }
    }
    */
}


public static class MorseMath
{
    public static double CalculateMedian(IEnumerable<double> source)
    {
        if (source == null || !source.Any()) return 0;

        var sortedList = source.OrderBy(n => n).ToList();
        int count = sortedList.Count;
        int midpoint = count / 2;

        if (count % 2 == 0)
            return (sortedList[midpoint - 1] + sortedList[midpoint]) / 2.0;

        return sortedList[midpoint];
    }

    public static double CalculateVariance(IEnumerable<double> values)
    {
        if (values == null || !values.Any()) return 0;

        double avg = values.Average();
        // Variance is the average of the squared deviations from the mean
        double sumOfSquares = values.Select(v => Math.Pow(v - avg, 2)).Sum();

        return sumOfSquares / values.Count();
    }

    public static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        // Standard Deviation is just the square root of Variance
        return Math.Sqrt(CalculateVariance(values));
    }

}



public static class MorseTable
{
    private static readonly Dictionary<string, char> _map = new Dictionary<string, char>
    {
        // Letters
        {".-", 'A'}, {"-...", 'B'}, {"-.-.", 'C'}, {"-..", 'D'}, {".", 'E'},
        {"..-.", 'F'}, {"--.", 'G'}, {"....", 'H'}, {"..", 'I'}, {".---", 'J'},
        {"-.-", 'K'}, {".-..", 'L'}, {"--", 'M'}, {"-.", 'N'}, {"---", 'O'},
        {".--.", 'P'}, {"--.-", 'Q'}, {".-.", 'R'}, {"...", 'S'}, {"-", 'T'},
        {"..-", 'U'}, {"...-", 'V'}, {".--", 'W'}, {"-..-", 'X'}, {"-.--", 'Y'},
        {"--..", 'Z'},

        // Numbers
        {"-----", '0'}, {".----", '1'}, {"..---", '2'}, {"...--", '3'},
        {"....-", '4'}, {".....", '5'}, {"-....", '6'}, {"--...", '7'},
        {"---..", '8'}, {"----.", '9'},

        // Punctuation & Prosigns
        {".-.-.-", '.'}, {"--..--", ','}, {"..--..", '?'}, {".----.", '\''},
        {"-.-.--", '!'}, {"-..-.", '/'}, {"-.--.", '('}, {"-.--.-", ')'},
        {".-...", '&'}, {"---...", ':'}, {"-....-", '-'}, {".-..-.", '"'},
        {".--.-.", '@'}, {"-...-", '='}, {".-.-.", '+'}, // AR (End of Message)
        {"...-.-", '|'}, // SK (End of Work) - Using pipe as a placeholder
    };

    public static char Lookup(string sequence)
    {
        if (_map.TryGetValue(sequence, out char result))
        {
            return result;
        }
        return '~'; // Return a tilda mark for unknown sequences
    }
}