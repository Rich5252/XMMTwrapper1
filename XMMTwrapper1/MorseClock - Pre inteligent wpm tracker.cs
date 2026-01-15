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

    // --- AUTOTRACKER STATE ---
    private List<double> _recentDotLengths = new List<double>();
    private const int MaxTrackedDots = 5;
    // -------------------------

    public Action<string> OnSequenceChanged;
    public Action<char> OnCharacterDecoded;
    private readonly object _lock = new object();

    public void SetSampleRate(int rate) => _sampleRate = rate;

    public long GetTotalSamples() => _totalSamplesProcessed;

    public void SetDotLength(double ms)
    {
        lock (_lock)
        {
            _dotMs = ms;
            _dotMsDefault = ms;
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
        bool isDash = adjustedDuration > _dotMs * 2.0;

        // --- AUTOTRACKER UPDATE ---
        // If it's a dot and not a glitch, learn from it
        if (!isDash && adjustedDuration > 10)
        {
            _recentDotLengths.Add(adjustedDuration);
            if (_recentDotLengths.Count > MaxTrackedDots) _recentDotLengths.RemoveAt(0);

            // Calculate new average dot length
            double sum = 0;
            foreach (var d in _recentDotLengths) sum += d;
            _dotMs = sum / _recentDotLengths.Count;
            if (_dotMs > 1200/10 || _dotMs < 1200/40) _dotMs = _dotMsDefault;
        }
        // -------------------------

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