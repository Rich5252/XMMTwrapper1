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

            if (isTonePresent == _lastState)
            {
                if (!isTonePresent && _currentSymbolBuffer.Length > 0)
                {
                    double silenceMs = GetElapsedMs();
                    if (silenceMs > _dotMs * 3.5) DecodeBuffer();
                }
                return;
            }

            double elapsedMs = GetElapsedMs();

            // Adaptive Debounce (Noise Filter)
            if (elapsedMs < _dotMs * 0.4)
            {
                _lastState = isTonePresent;
                return;
            }

            if (_lastState && !isTonePresent) ProcessMark((long)elapsedMs);
            else if (!_lastState && isTonePresent) ProcessSpace((long)elapsedMs);

            _lastState = isTonePresent;
            _lastTransitionSample = _totalSamplesProcessed;
        }
    }

    private double GetElapsedMs() => ((_totalSamplesProcessed - _lastTransitionSample) / _sampleRate) * 1000.0;
    private void ProcessMark(long duration)
    {
        // 40WPM Compensation (Filter lag)
        long adjustedDuration = duration + 10;

        // Classification threshold
        bool isDash = adjustedDuration > _dotMs * 2.25;

        // --- AUTOTRACKER UPDATE ---
        if (adjustedDuration > 10)
        {
            // 1. Normalize the duration
            double dotEquivalent = isDash ? (adjustedDuration / 3.0) : adjustedDuration;

            // 2. Add to history
            _recentDotLengths.Add(dotEquivalent);
            if (_recentDotLengths.Count > MaxTrackedDots) _recentDotLengths.RemoveAt(0);

            // 3. MEDIAN calculation (Ignores outliers/spurious "Es")
            double medianDot = CalculateMedian(_recentDotLengths);

            // 4. SANITY CHECK & BOUNDS (30ms = 40WPM, 100ms = 12WPM)
            if (medianDot >= 25 && medianDot <= 120)
            {
                // 5. ASYMMETRIC SMOOTHING (Dash-Weighting)
                // We trust a Dash more than a Dot because noise rarely fakes a long Dash.
                double weight = isDash ? 0.15 : 0.05;

                // 6. JITTER GATE
                // Only adjust if the change is significant (> 5%)
                if (Math.Abs(medianDot - _dotMs) > (_dotMs * 0.05))
                {
                    _dotMs = (_dotMs * (1.0 - weight)) + (medianDot * weight);
                }
            }
            else
            {
                // Slowly drift back to default if signal is lost or garbage
                _dotMs = (_dotMs * 0.99) + (_dotMsDefault * 0.01);
            }

            // 7. UI DEBOUNCE
            int newWpm = (int)(1200 / _dotMs);
            if ((int)_currentWPM != newWpm)
            {
                _currentWPM = newWpm;
                OnWpmChanged?.Invoke((int)_currentWPM);
            }
        }

        if (isDash) _currentSymbolBuffer.Append("-");
        else _currentSymbolBuffer.Append(".");

        OnSequenceChanged?.Invoke(_currentSymbolBuffer.ToString());
    }

    private void ProcessSpace(long duration)
    {
        // Now using the tracked _dotMs for more accurate gaps
        if (duration > _dotMs * 1.5) DecodeBuffer();
        if (duration > _dotMs * 5.0) OnCharacterDecoded?.Invoke(' ');
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
        double stableDotLength = CalculateMedian(_elementHistory);

        // WPM = 1200 / dotLengthInMs
        _currentWPM = 1200.0 / stableDotLength;
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

    private double CalculateMedian(List<double> values)
    {
        if (values == null || values.Count == 0) return 0;

        // 1. Create a copy so we don't mess up the chronological history
        double[] sortedCopy = values.ToArray();

        // 2. Sort the copy
        Array.Sort(sortedCopy);

        int count = sortedCopy.Length;
        if (count % 2 == 0)
        {
            // Even number of elements: average the two middle ones
            return (sortedCopy[count / 2 - 1] + sortedCopy[count / 2]) / 2.0;
        }
        else
        {
            // Odd number of elements: take the middle one
            return sortedCopy[count / 2];
        }
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