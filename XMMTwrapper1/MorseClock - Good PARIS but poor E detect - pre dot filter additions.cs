// ***************************************************
//Strategy Header: Statistical Rhythm Adaptive Decoder
// ***************************************************
//Core Logic: This engine utilizes a Dual-Bucket Median Tracking strategy to determine Morse speed.
//Unlike decoders that only track the length of "dots," this engine tracks both Mark Dots and Element
//Spaces independently. By calculating the median of these two buckets and averaging them, the engine
//derives a "True Dot Unit" that is resistant to "weighting" (where a radio or keyer makes beeps
//longer than the silences between them).

//Key Parameter Definitions:

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

//Speed Stability Strategy: The engine maintains a history of the last 25 elements (Buckets).
//  By using the Median rather than the Mean, the engine inherently ignores "outliers" such as spurious
//  noise spikes (extra-short dots) or signal fades (extra-long elements) that would otherwise cause "WPM bounce."

// ***************************************************************************************************************

using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class MorseClock
{
    // --- TUNING CONSTANTS (The "Engine" Settings) ---
    private static class Config
    {
        // MARK CLASSIFICATION
        // Increased to 2.5 to prevent "Heavy Dots" from becoming "Dashes"
        public const double DASH_THRESHOLD_RATIO = 2.5;

        // SPACE CLASSIFICATION
        // Increased to 2.4 to prevent "Heavy Spaces" from splitting letters
        public const double ELEMENT_SPACE_MAX_RATIO = 2.3;
        public const double LETTER_SPACE_MIN_RATIO = 2.4;
        public const double WORD_SPACE_MIN_RATIO = 5.5;

        // TIMING & DEBOUNCE
        public const double DEBOUNCE_RATIO = 0.25;       //was 0.25
        public const double IDLE_DECODE_RATIO = 3.5;      // More patient idle timer
        public const int FILTER_COMPENSATION_MS = 2;      // Start low and use Calibration tool

        // STATISTICS
        public const int HISTORY_BUCKET_SIZE = 100;     //was 25
        public const double ADAPTATION_BASE_WEIGHT = 0.04;
    }

    private double _sampleRate = 48000.0;
    private long _totalSamplesProcessed = 0;
    private long _lastTransitionSample = 0;
    private bool _lastState = false;
    private StringBuilder _currentSymbolBuffer = new StringBuilder();

    private double _dotMs = 1200.0 / 25.0;
    private double _currentWPM = 25;

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

    public void SetDotLength(double ms)
    {
        lock (_lock)
        {
            _dotMs = ms;
            _currentWPM = 1200 / ms;
        }
    }

    public void UpdateState(bool isTonePresent, int samplesInBlock)
    {
        lock (_lock)
        {
            _totalSamplesProcessed += samplesInBlock;

            // 1. Idle/Same State Management
            if (isTonePresent == _lastState)
            {
                if (!isTonePresent && _currentSymbolBuffer.Length > 0)
                {
                    double silenceMs = GetElapsedMs();
                    if (silenceMs > _dotMs * Config.IDLE_DECODE_RATIO) DecodeBuffer();
                }
                return;
            }

            // 2. Measure transition
            double elapsedMs = GetElapsedMs();

            // 3. Adaptive Debounce
            if (elapsedMs < _dotMs * Config.DEBOUNCE_RATIO) return;

            // 4. Dispatch with Symmetrical Compensation
            // We add to both because the filter lengthens marks AND effectively 
            // shortens the detected start of the next silence.
            long adjusted = (long)elapsedMs + Config.FILTER_COMPENSATION_MS;

            if (_lastState && !isTonePresent)
                ProcessMark(adjusted);
            else if (!_lastState && isTonePresent)
                ProcessSpace(adjusted);

            _lastState = isTonePresent;
            _lastTransitionSample = _totalSamplesProcessed;
        }
    }

    private void ProcessMark(long duration)
    {
        bool isDash = duration > (_dotMs * Config.DASH_THRESHOLD_RATIO);

        if (isDash)
        {
            _dashHistory.Add(duration);
            if (_dashHistory.Count > Config.HISTORY_BUCKET_SIZE) _dashHistory.RemoveAt(0);
        }
        else
        {
            _dotHistory.Add(duration);
            if (_dotHistory.Count > Config.HISTORY_BUCKET_SIZE) _dotHistory.RemoveAt(0);
        }

        _currentSymbolBuffer.Append(isDash ? "-" : ".");
        OnSequenceChanged?.Invoke(_currentSymbolBuffer.ToString());
        UpdateSpeedTracking();
    }

    private void ProcessSpace(long duration)
    {
        // Tier 1: Element Space (Internal Timing)
        if (duration < _dotMs * Config.LETTER_SPACE_MIN_RATIO)
        {
            _spaceHistory.Add((double)duration);
            if (_spaceHistory.Count > Config.HISTORY_BUCKET_SIZE) _spaceHistory.RemoveAt(0);
            UpdateSpeedTracking();
        }
        // Tier 2: Letter Space (Trigger Decode)
        else if (duration >= _dotMs * Config.LETTER_SPACE_MIN_RATIO && duration < _dotMs * Config.WORD_SPACE_MIN_RATIO)
        {
            DecodeBuffer();
        }
        // Tier 3: Word Space
        else if (duration >= _dotMs * Config.WORD_SPACE_MIN_RATIO)
        {
            DecodeBuffer();
            OnCharacterDecoded?.Invoke(' ');
        }
    }

    private void UpdateSpeedTracking()
    {
        if (_dotHistory.Count < 3 || _spaceHistory.Count < 3) return;

        double medianDotMark = MorseMath.CalculateMedian(_dotHistory);
        double medianSpace = MorseMath.CalculateMedian(_spaceHistory);

        // Average of beep-dot and silence-dot provides the stablest base
        double currentMeasuredDot = (medianDotMark + medianSpace) / 2.0;

        double dotVar = MorseMath.CalculateStandardDeviation(_dotHistory);
        double consistency = Math.Max(0, 1.0 - (dotVar / medianDotMark));

        double weight = Config.ADAPTATION_BASE_WEIGHT + (consistency * 0.10);
        _dotMs = (_dotMs * (1.0 - weight)) + (currentMeasuredDot * weight);

        double newWpm = 1200.0 / _dotMs;
        if (newWpm > 40 || newWpm < 15) newWpm = 25;
        if ((int)_currentWPM != (int)newWpm)
        {
            _currentWPM = newWpm;
            OnWpmChanged?.Invoke((int)_currentWPM);
        }
    }

    private void DecodeBuffer()
    {
        string sequence = _currentSymbolBuffer.ToString();
        if (string.IsNullOrEmpty(sequence)) return;

        char result = MorseTable.Lookup(sequence);
        if (result != '\0') OnCharacterDecoded?.Invoke(result);

        _currentSymbolBuffer.Clear();
        OnSequenceChanged?.Invoke("");
    }

    private double GetElapsedMs() => ((_totalSamplesProcessed - _lastTransitionSample) / _sampleRate) * 1000.0;
}

public class MorseTracker
{
    private List<double> _dotHistory = new();
    private List<double> _dashHistory = new();
    private List<double> _spaceHistory = new(); // The "Secret Weapon"

    public double TrueDotMs { get; private set; } = 50.0; // Starts at ~24 WPM
    public double Confidence { get; private set; } = 0;

    public void ProcessElement(long duration, bool isMark)
    {
        if (duration <= 0) return;

        if (isMark)
        {
            // Classification based on current TrueDotMs
            if (duration > TrueDotMs * 2.0)
                UpdateHistory(_dashHistory, duration);
            else
                UpdateHistory(_dotHistory, duration);
        }
        else
        {
            // Only track "Element Spaces" (the gap between dits/dahs)
            // Word spaces (> 5x dot) should be ignored for WPM tracking
            if (duration < TrueDotMs * 2.5)
                UpdateHistory(_spaceHistory, duration);
        }

        RecalculateRhythm();
    }

    private void UpdateHistory(List<double> list, double value)
    {
        list.Add(value);
        if (list.Count > 30) list.RemoveAt(0);
    }

    private void RecalculateRhythm()
    {
        if (_dotHistory.Count < 5 || _spaceHistory.Count < 5) return;

        double medianDot = MorseMath.CalculateMedian(_dotHistory);
        double medianDash = MorseMath.CalculateMedian(_dashHistory);
        double medianSpace = MorseMath.CalculateMedian(_spaceHistory);

        // --- THE INTELLIGENT STEP ---
        // In a perfect world, medianDot == medianSpace. 
        // In a "heavy" fist, medianDot > medianSpace.
        // We calculate WPM based on the average of all "single unit" elements.

        double measuredDotBase = (medianDot + medianSpace) / 2.0;

        // Calculate actual ratio (should be 3.0)
        double actualRatio = _dashHistory.Count > 5 ? (medianDash / measuredDotBase) : 3.0;

        // Confidence: How tight is the grouping? 
        // High variance in history = Low confidence (hand keying)
        double variance = MorseMath.CalculateVariance(_dotHistory);
        Confidence = Math.Max(0, 1.0 - (variance / measuredDotBase));

        // Apply Smoothing: Trust the data more if Confidence is high
        double adaptRate = 0.05 + (Confidence * 0.10);
        TrueDotMs = (TrueDotMs * (1.0 - adaptRate)) + (measuredDotBase * adaptRate);
    }
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
        return '?'; // Return a question mark for unknown sequences
    }
}