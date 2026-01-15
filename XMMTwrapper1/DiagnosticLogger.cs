using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class Log
{
    // ---- Configuration ----
    // Set this at startup if you want a file.
    // Example: Log.SetLogFile(Path.Combine(AppContext.BaseDirectory, "startup.log"));
    private static string? _logFilePath;

    // If true, writes to Debug/Trace output (Output window).
    private static volatile bool _debugOutEnabled = true;

    // If true, writes to file (if _logFilePath != null).
    private static volatile bool _fileOutEnabled = false;

    // ---- Internals ----
    private static readonly ConcurrentQueue<string> _q = new();
    private static readonly AutoResetEvent _signal = new(false);
    private static readonly CancellationTokenSource _cts = new();

    private static int _started; // 0/1

    // Call once very early (Program.Main before Application.Run)
    public static void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0) return;

        // Background flush loop
        Task.Factory.StartNew(
            FlushLoop,
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );

        // Ensure we flush on exit
        AppDomain.CurrentDomain.ProcessExit += (_, __) => StopAndFlush();
        Application.ApplicationExit += (_, __) => StopAndFlush();
    }

    public static void SetLogFile(string path, bool enableFile = true)
    {
        _logFilePath = path;
        _fileOutEnabled = enableFile;
    }

    public static void EnableDebugOutput(bool enabled) => _debugOutEnabled = enabled;
    public static void EnableFileOutput(bool enabled) => _fileOutEnabled = enabled;

    public static void StopAndFlush()
    {
        if (Interlocked.Exchange(ref _started, 0) == 0) return;

        try
        {
            _cts.Cancel();
            _signal.Set();
            // Best-effort: give worker a moment to drain without blocking forever
            DrainOnce();
        }
        catch { /* swallow */ }
    }

    // ---- Public API (your methods, refactored) ----

    // No [Conditional] attribute means this stays in RELEASE mode
    public static void Perf(string message, string tag = "QSB")
    {
        // We still use Enq so it stays on the background thread
        Enq(FormatLine(message: message, tag: tag));
    }

    [Conditional("DEBUG")]
    public static void Msg(
        string message,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        Enq(FormatLine(
            message: $"{Path.GetFileName(file)}:{line} {member} - {message}",
            tag: null
        ));
    }

    [Conditional("DEBUG")]
    public static void Mark(string s)
    {
        Enq(FormatLine(message: s, tag: null));
    }

    [Conditional("DEBUG")]
    public static void LogThread(string where)
    {
        var t = Thread.CurrentThread;
        Enq(FormatLine(
            message: $"[{where}] T{t.ManagedThreadId} Apt={t.GetApartmentState()} IsThreadPool={t.IsThreadPoolThread}",
            tag: null
        ));
    }

    [Conditional("DEBUG")]
    public static void LogThreadDetails(string tag)
    {
        var t = Thread.CurrentThread;
        Enq(FormatLine(
            message: $"[{tag}] T{t.ManagedThreadId} Apt={t.GetApartmentState()} IsThreadPool={t.IsThreadPoolThread}",
            tag: null
        ));
    }

    [Conditional("DEBUG")]
    public static void LogThreadDetails(string tag, Control ctrl)
    {
        var t = Thread.CurrentThread;
        bool handleCreated = false;
        bool invokeRequired = false;

        try
        {
            handleCreated = ctrl.IsHandleCreated;
            if (handleCreated)
                invokeRequired = ctrl.InvokeRequired;
        }
        catch
        {
            // accessing Control props during shutdown can throw; ignore
        }

        Enq(FormatLine(
            message: $"[{tag}] T{t.ManagedThreadId} Apt={t.GetApartmentState()} IsThreadPool={t.IsThreadPoolThread} HandleCreated={handleCreated} InvokeRequired={invokeRequired}",
            tag: null
        ));
    }

    // Optional: if you want explicit tagged lines like your sink had:
    public static void Tagged(string tag, string message)
    {
        Enq(FormatLine(message: message, tag: tag));
    }

    // ---- Helpers ----

    private static void Enq(string line)
    {
        // Ensure background worker is running even if you forgot Start()
        if (Volatile.Read(ref _started) == 0) Start();

        _q.Enqueue(line);
        _signal.Set();
    }

    private static string FormatLine(string message, string? tag)
    {
        int tid = Environment.CurrentManagedThreadId;
        string ts = DateTime.Now.ToString("HH:mm:ss.fff");

        if (!string.IsNullOrEmpty(tag))
            return $"{ts} [{tag}] T{tid} {message}";

        return $"{ts} [T{tid}] {message}";
    }

    private static void FlushLoop()
    {
        var token = _cts.Token;

        while (!token.IsCancellationRequested)
        {
            // Wait until signaled or timeout to batch writes
            _signal.WaitOne(50);
            DrainOnce();
        }

        // final drain
        DrainOnce();
    }

    private static void DrainOnce()
    {
        if (_q.IsEmpty) return;

        // Batch into one string for file writes (fewer syscalls)
        using var sw = _fileOutEnabled && !string.IsNullOrWhiteSpace(_logFilePath)
            ? new StreamWriter(new FileStream(_logFilePath!, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            : null;

        while (_q.TryDequeue(out var line))
        {
            try
            {
                if (_debugOutEnabled)
                {
                    // Trace is often less “sticky” than Debug depending on listeners
                    Trace.WriteLine(line);
                }

                sw?.WriteLine(line);
            }
            catch
            {
                // never let logging crash or deadlock the app
            }
        }

        try { sw?.Flush(); } catch { }
    }
}
