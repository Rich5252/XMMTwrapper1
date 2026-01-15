using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Timers;

public class MorseClock
{
    private double _sampleRate = 48000.0; // Default
    private long _totalSamplesProcessed = 0;
    private long _lastTransitionSample = 0;
    private bool _lastState = false;
    private StringBuilder _currentSymbolBuffer = new StringBuilder();
    private double _dotMs = 120; // Target WPM

    public Action<string> OnSequenceChanged;
    public Action<char> OnCharacterDecoded;

    private readonly object _lock = new object();

    public void SetSampleRate(int rate) => _sampleRate = rate;
    public void SetDotLength(double ms) => _dotMs = ms;

    public void UpdateState(bool isTonePresent, int samplesInBlock)
    {
        lock (_lock)
        {
            _totalSamplesProcessed += samplesInBlock;

            // 1. Same-state logic (Watchdog)
            if (isTonePresent == _lastState)
            {
                if (!isTonePresent && _currentSymbolBuffer.Length > 0)
                {
                    double silenceMs = GetElapsedMs();
                    if (silenceMs > _dotMs * 3.5) DecodeBuffer();
                }
                return;
            }

            // 2. State Change logic
            double elapsedMs = GetElapsedMs();

            // Adaptive Debounce (40% of dot length)
            // At 40WPM (30ms dot), this is 12ms.
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

    private double GetElapsedMs()
    {
        return ((_totalSamplesProcessed - _lastTransitionSample) / _sampleRate) * 1000.0;
    }

    private void ProcessMark(long duration)
    {
        // Compensation: 10-15ms for filter rise time at 48kHz
        long adjustedDuration = duration + 12;

        if (adjustedDuration > _dotMs * 2.0) _currentSymbolBuffer.Append("-");
        else _currentSymbolBuffer.Append(".");

        OnSequenceChanged?.Invoke(_currentSymbolBuffer.ToString());
    }

    private void ProcessSpace(long duration)
    {
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