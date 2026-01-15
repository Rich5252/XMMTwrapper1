using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

public class MorseClock
{
    private Stopwatch _timer = new Stopwatch();
    private bool _lastState = false;
    private StringBuilder _currentSymbolBuffer = new StringBuilder();

    // The 'Unit' (length of a dot) in milliseconds. 
    // 60ms is roughly 20 WPM.
    private double _dotMs = 60;

    // Events to send data back to the Engine/UI
    public Action<string> OnSequenceChanged; // e.g., sends ".-."
    public Action<char> OnCharacterDecoded;   // e.g., sends 'R'

    private readonly object _lock = new object();

    private long _silenceAccumulator = 0;

    public MorseClock()
    {
        _timer.Start();
    }
    
    public void UpdateState(bool isTonePresent)
    {
        lock (_lock)
        {
            // 1. If the state reported by the engine matches our internal state, do nothing.
            if (isTonePresent == _lastState) return;

            long elapsed = _timer.ElapsedMilliseconds;

            // 2. DEBOUNCE / NOISE FILTER
            // If the state switched too fast (e.g., 0ms or 10ms), it's a glitch.
            if (elapsed < 30)
            {
                // IMPORTANT: We update the state so the 'if' above catches it next time,
                // but we do NOT Restart the timer and do NOT call ProcessMark.
                // We are essentially "merging" this tiny glitch into the previous state.
                _lastState = isTonePresent;
                return;
            }

            // 3. VALID TRANSITION (Passed the 40ms test)
            if (_lastState == true && isTonePresent == false)
            {
               //Log.Mark($"Mark Finished: {elapsed}ms");
                // We were toning, now we are silent. 
                ProcessMark(elapsed);
            }
            else if (_lastState == false && isTonePresent == true)
            {
                // We were silent, now we are toning.
                ProcessSpace(elapsed);
            }

            // 4. COMMIT & RESET
            _lastState = isTonePresent;
            _timer.Restart();
        }
    }

    private void ProcessMark(long duration)
    {
        // Add back the time lost to filter ramp-up and squelch.
        // Based on your logs (120ms - 70ms), your offset is about 50ms.
        long adjustedDuration = duration + 40;

        Log.Mark($"Mark Duration: {adjustedDuration}ms (Target Dot: {_dotMs}ms)");
        
        // Dash is usually 3x Dot. We use 1.5x as the 'threshold'
        if (adjustedDuration > _dotMs * 1.5)
            _currentSymbolBuffer.Append("-");
        else
            _currentSymbolBuffer.Append(".");



        OnSequenceChanged?.Invoke(_currentSymbolBuffer.ToString());

        // Adaptive speed: Update the dot length based on recent dots
        // (Advanced: You can average the last 5 dots here)
    }

    private void ProcessSpace(long duration)
    {
        Log.Mark($"Space Duration: {duration}ms (Target Space: {_dotMs * 2.5}ms)");
        // If silence > 2.5 dots, the character is finished
        if (duration > _dotMs * 2.5)
        {
            DecodeBuffer();
        }

        // If silence > 6 dots, it's a word space
        if (duration > _dotMs * 6.0)
        {
            OnCharacterDecoded?.Invoke(' ');
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

    public void SetDotLength(double ms)
    {
        _dotMs = ms;
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