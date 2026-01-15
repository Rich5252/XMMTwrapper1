using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Timers;

public class MorseClock
{
    private Stopwatch _timer = new Stopwatch();
    private bool _lastState = false;
    private StringBuilder _currentSymbolBuffer = new StringBuilder();
    private double _dotMs = 120; // Default to 10 WPM

    public Action<string> OnSequenceChanged;
    public Action<char> OnCharacterDecoded;

    private readonly object _lock = new object();
    private System.Timers.Timer _watchdog;

    public MorseClock()
    {
        _timer.Start();

        // Initialize Watchdog to check every 100ms
        _watchdog = new System.Timers.Timer(100);
        _watchdog.Elapsed += CheckForInactivity;
        _watchdog.AutoReset = true;
        _watchdog.Enabled = true;
    }

    public void UpdateState(bool isTonePresent)
    {
        lock (_lock)
        {
            if (isTonePresent == _lastState) return;

            long elapsed = _timer.ElapsedMilliseconds;

            // Debounce: Ignore very fast flickers but update state to "absorb" them
            if (elapsed < 25)
            {
                _lastState = isTonePresent;
                return;
            }

            if (_lastState == true && isTonePresent == false)
            {
                ProcessMark(elapsed);
            }
            else if (_lastState == false && isTonePresent == true)
            {
                ProcessSpace(elapsed);
            }

            _lastState = isTonePresent;
            _timer.Restart(); // Correct placement: Only restart after a valid transition
        }
    }

    private void ProcessMark(long duration)
    {
        // Compensation: Add back ~50ms lost to Goertzel ramp-up/Squelch
        long adjustedDuration = duration + 50;

        // Dash vs Dot decision at 2.0 units
        if (adjustedDuration > _dotMs * 2.0)
            _currentSymbolBuffer.Append("-");
        else
            _currentSymbolBuffer.Append(".");

        OnSequenceChanged?.Invoke(_currentSymbolBuffer.ToString());
    }

    private void ProcessSpace(long duration)
    {
        // Letter Space Trigger: Using 1.5 units to catch gaps effectively
        if (duration > _dotMs * 1.5)
        {
            DecodeBuffer();
        }

        // Word Space Trigger: 5.0 units
        if (duration > _dotMs * 5.0)
        {
            OnCharacterDecoded?.Invoke(' ');
        }
    }

    private void CheckForInactivity(object sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            // If we are in silence and the buffer isn't empty
            if (!_lastState && _currentSymbolBuffer.Length > 0)
            {
                if (_timer.ElapsedMilliseconds > _dotMs * 3.0)
                {
                    DecodeBuffer();
                }
            }
        }
    }

    private void DecodeBuffer()
    {
        string sequence = _currentSymbolBuffer.ToString();
        if (string.IsNullOrEmpty(sequence)) return;

        char result = MorseTable.Lookup(sequence);
        OnCharacterDecoded?.Invoke(result);

        _currentSymbolBuffer.Clear();
        OnSequenceChanged?.Invoke("");
    }

    public void SetDotLength(double ms) => _dotMs = ms;
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