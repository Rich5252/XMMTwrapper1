using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Timers;

public class MorseClock
{
    private double _sampleRate = 48000.0;
    private long _totalSamplesProcessed = 0;
    private long _lastTransitionSample = 0;
    private bool _lastState = false;
    private StringBuilder _currentSymbolBuffer = new StringBuilder();
    private double _dotMs = 1200/25;            // This will now be dynamic
    private double _dotMsDefault = 1200/25;     //25wpm
    private double _currentWPM = 25;            //25 wpm

    // --- AUTOTRACKER STATE ---
    private List<double> _recentDotLengths = new List<double>();
    private const int MaxTrackedDots = 5;
    // -------------------------

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
            _dotMsDefault = ms;
            _currentWPM = 1200 / ms;
            _recentDotLengths.Clear(); // Reset tracker when manual WPM changes
        }
    }

    public void UpdateState(bool isTonePresent, int samplesInBlock)
    {
        lock (_lock)
        {
            _totalSamplesProcessed += samplesInBlock;

            // 1. Same State: Check for Word/Character gaps
            if (isTonePresent == _lastState)
            {
                if (!isTonePresent && _currentSymbolBuffer.Length > 0)
                {
                    double silenceMs = GetElapsedMs();
                    // Word space detection (Standard Morse is 7 dots, but 3.5-4 is safe for decoding)
                    if (silenceMs > _dotMs * 2.8) DecodeBuffer();
                }
                return;
            }

            // 2. State Changed: Measure the element that just finished
            double elapsedMs = GetElapsedMs();

            // 3. Adaptive Debounce (Noise Filter)
            // If the pulse is too short to be a real Dit, we treat it as noise.
            if (elapsedMs < _dotMs * 0.3)
            {
                // Ignore this transition entirely. 
                // Do NOT update _lastState or _lastTransitionSample.
                // This "absorbs" the noise spike into the current state.
                return;
            }

            // 4. Dispatch to Statistics Logic
            if (_lastState && !isTonePresent)
                ProcessMark((long)elapsedMs + 3); // Beep was shortened by filter ramp-up
            else if (!_lastState && isTonePresent)
                ProcessSpace((long)elapsedMs + 3); // Silence was eaten by filter tail
            /*
            if (_lastState)
            {
                // Transition: High -> Low (Mark ended)
                ProcessMark((long)elapsedMs);
            }
            else
            {
                // Transition: Low -> High (Space ended)
                ProcessSpace((long)elapsedMs);
            }
            */

            // 5. Commit the state change
            _lastState = isTonePresent;
            _lastTransitionSample = _totalSamplesProcessed;
        }
    }

    private double GetElapsedMs() => ((_totalSamplesProcessed - _lastTransitionSample) / _sampleRate) * 1000.0;


    private List<double> _dotHistory = new();   // Only Marks classified as Dots
    private List<double> _dashHistory = new();  // Only Marks classified as Dashes
    private List<double> _spaceHistory = new(); // Only Silences < 2.0 * _dotMs

    private void ProcessMark(long duration)
    {
        double adjusted = duration;  // + 5; // Reduced from 10 to prevent 'JMQMO' (over-dashing)

        // 1. Classification based on the stable tracker
        bool isDash = adjusted > (_dotMs * 2.4);

        // 2. Update the appropriate history bucket
        if (isDash)
        {
            _dashHistory.Add(adjusted);
            if (_dashHistory.Count > 20) _dashHistory.RemoveAt(0);
        }
        else
        {
            _dotHistory.Add(adjusted);
            if (_dotHistory.Count > 20) _dotHistory.RemoveAt(0);
        }

        // 3. Update the Symbol Buffer
        _currentSymbolBuffer.Append(isDash ? "-" : ".");

        // 4. Run the Statistics Engine
        UpdateSpeedTracking();
    }

    private void ProcessSpace(long duration)
    {
        // 1. Element Space (Gap between dits/dahs)
        // Range: 0.5 to 2.2. If it's in this range, it's NOT a letter break.
        if (duration < _dotMs * 2)
        {
            _spaceHistory.Add((double)duration);
            if (_spaceHistory.Count > 20) _spaceHistory.RemoveAt(0);
            UpdateSpeedTracking();
        }
        // 2. Letter Space (The "Sweet Spot")
        // Range: 2.2 to 5.0. 
        else if (duration >= _dotMs * 2 && duration < _dotMs * 5.0)
        {
            DecodeBuffer();
        }
        // 3. Word Space
        else if (duration >= _dotMs * 5.0)
        {
            DecodeBuffer();
            OnCharacterDecoded?.Invoke(' ');
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

    private List<double> _elementHistory = new List<double>();
    private const int HistorySize = 9; // Large enough to hold "PARIS"

    public void RegisterElement(double lengthInMs, bool isDash)
    {
        // Normalize dashes to "dot-equivalents" for the math
        double dotEquivalent = isDash ? lengthInMs / 3.0 : lengthInMs;

        _elementHistory.Add(dotEquivalent);
        if (_elementHistory.Count > HistorySize) _elementHistory.RemoveAt(0);

        // Use the Median of the history to calculate WPM
        double stableDotLength = MorseMath.CalculateMedian(_elementHistory);

        // WPM = 1200 / dotLengthInMs
        double newWpm = 1200.0 / stableDotLength;
        if ((int)_currentWPM != newWpm)
        {
            _currentWPM = newWpm;
            OnWpmChanged?.Invoke((int)_currentWPM);
        }
    }

    public void OnElementDetected(double lengthInMs)
    {
        double expectedDot = 1200.0 / _currentWPM;

        // 1. Is it a "Micro-Dot"? (Noise Spike)
        if (lengthInMs < expectedDot * 0.4)
        {
            // This is likely a spurious 'E' or static. 
            // Log it, but DO NOT update the WPM tracker.
            return;
        }

        // 2. Is it a "Mega-Dot"? 
        // If it's too long for a dot but too short for a dash, 
        // it's likely a signal hit by a noise fade.

        // 3. Only update WPM if the element is within a 'Reasonable' range (e.g. +/- 50%)
    }

    private void UpdateSpeedTracking()
    {
        if (_dotHistory.Count < 3 || _spaceHistory.Count < 3) return;

        double medianDotMark = MorseMath.CalculateMedian(_dotHistory);
        double medianSpace = MorseMath.CalculateMedian(_spaceHistory);

        // The "True Dot" is the average of a beep-dot and a silence-dot
        double currentMeasuredDot = (medianDotMark + medianSpace) / 2.0;

        // Consistency Check
        double dotVar = MorseMath.CalculateStandardDeviation(_dotHistory);
        double consistency = Math.Max(0, 1.0 - (dotVar / medianDotMark));

        // Smoothing
        double weight = 0.05 + (consistency * 0.10);
        _dotMs = (_dotMs * (1.0 - weight)) + (currentMeasuredDot * weight);

        // Update WPM
        double newWpm = 1200 / _dotMs;
        if ((int)_currentWPM != newWpm)
        {
            _currentWPM = newWpm;
            OnWpmChanged?.Invoke((int)_currentWPM);
        }

    }
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