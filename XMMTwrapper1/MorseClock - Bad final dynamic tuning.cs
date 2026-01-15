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
        public const double DASH_THRESHOLD_RATIO = 2.6;

        // SPACE CLASSIFICATION
        // Increased to 2.4 to prevent "Heavy Spaces" from splitting letters
        public const double ELEMENT_SPACE_MAX_RATIO = 1.8;
        public const double LETTER_SPACE_MIN_RATIO = 2.4;
        public const double WORD_SPACE_MIN_RATIO = 5.5;

        // TIMING & DEBOUNCE
        public const double DEBOUNCE_RATIO = 0.25;
        public const double IDLE_DECODE_RATIO = 3.0;      // More patient idle timer
        public const int FILTER_COMPENSATION_MS = 3;      // Start low and use Calibration tool

        // STATISTICS
        public const int HISTORY_BUCKET_SIZE = 20;
        public const double ADAPTATION_BASE_WEIGHT = 0.05;
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
    public double _dynamicComp = 2.0;

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
            long adjusted = (long)elapsedMs + (long)_dynamicComp;       // Config.FILTER_COMPENSATION_MS;

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
        UpdateSpeedTracking();
    }

    private void ProcessSpace(long duration)
    {
        // Thresholds from your Config
        double elementMax = _dotMs * Config.ELEMENT_SPACE_MAX_RATIO; // 1.8x
        double letterMin = _dotMs * Config.LETTER_SPACE_MIN_RATIO;   // 2.4x

        // Anything under 2.4x is treated as an element space for tracking
        if (duration < letterMin)
        {
            // But we only add to history if it's a "clean" element space
            if (duration < elementMax)
            {
                _spaceHistory.Add((double)duration);
                if (_spaceHistory.Count > Config.HISTORY_BUCKET_SIZE) _spaceHistory.RemoveAt(0);
                UpdateSpeedTracking();
            }
        }
        // Anything over 2.4x is a Letter Break
        else if (duration >= letterMin && duration < _dotMs * Config.WORD_SPACE_MIN_RATIO)
        {
            DecodeBuffer();
        }
        // Word Space
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

        // 1. DYNAMIC CALIBRATION
        if (_dotHistory.Count >= 15 && _spaceHistory.Count >= 15)
        {
            double suggested = GetSuggestedCompensation();
            // Use a very high damping factor (0.999) for stability
            _dynamicComp = (_dynamicComp * 0.9995) + (suggested * 0.0005);
        }

        // 2. SPEED TRACKING
        // The "True Dot" is the average of the beep and the silence
        double currentMeasuredDot = (medianDotMark + medianSpace) / 2.0;

        double dotVar = MorseMath.CalculateStandardDeviation(_dotHistory);
        double consistency = Math.Max(0, 1.0 - (dotVar / medianDotMark));

        // Trust the measurement more if the sender is consistent
        double weight = Config.ADAPTATION_BASE_WEIGHT + (consistency * 0.10);
        _dotMs = (_dotMs * (1.0 - weight)) + (currentMeasuredDot * weight);

        // 3. WPM NOTIFICATION
        double newWpm = 1200.0 / _dotMs;
        if (newWpm > 30 || newWpm < 15) newWpm = 25;   
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

        OnSequenceChanged?.Invoke(sequence);            //always show current seq

        char result = MorseTable.Lookup(sequence);
        if (result != '\0') OnCharacterDecoded?.Invoke(result);

        _currentSymbolBuffer.Clear();
    }

    private double GetElapsedMs() => ((_totalSamplesProcessed - _lastTransitionSample) / _sampleRate) * 1000.0;

    public double GetSuggestedCompensation()
    {
        lock (_lock)
        {
            if (_dotHistory.Count < 10 || _spaceHistory.Count < 10) return 0;

            // Subtract current correction to find the raw hardware timing
            double medianMarkRaw = MorseMath.CalculateMedian(_dotHistory) - _dynamicComp;
            double medianSpaceRaw = MorseMath.CalculateMedian(_spaceHistory) - _dynamicComp;

            // The true imbalance is the difference between these raw values
            double imbalance = medianMarkRaw - medianSpaceRaw;

            // Centering the decision line requires half the total imbalance
            return imbalance / 2.0;
        }
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