using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// C# replacement for the C++ named pipe server. Matches NamedPipeClient's expectations:
///   - Same pipe name ("TXLinkCommandPipe" by default)
///   - PipeDirection.InOut (duplex)
///   - PipeTransmissionMode.Message  (client sets ReadMode = Message, which requires
///     the SERVER to have been created with PipeTransmissionMode.Message too)
///   - Async I/O
///
/// Usage:
///   var server = new NamedPipeServer();
///   server.CommandReceived += cmd => Console.WriteLine($"Got: {cmd}");
///   server.Connected += () => Console.WriteLine("Client connected");
///   server.Start();
///   ...
///   server.SendMessage("DOWN,5449868744");
/// </summary>
public class NamedPipeServer : IDisposable
{
    private readonly string _pipeName;
    private NamedPipeServerStream _pipeServer;
    private CancellationTokenSource _cts;
    private Task _listenTask;
    private readonly object _writeLock = new object();

    public event Action Connected;
    public event Action Disconnected;
    public event Action<string> CommandReceived;

    public bool IsConnected => _pipeServer?.IsConnected == true;

    public NamedPipeServer(string pipeName = "TXLinkCommandPipe")
    {
        _pipeName = pipeName;
    }

    /// <summary>Starts the accept/read loop on a background task. Safe to call once.</summary>
    public void Start()
    {
        if (_listenTask != null) return; // already running

        _cts = new CancellationTokenSource();
        _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _pipeServer?.Dispose(); } catch { /* already gone */ }
    }

    private async Task ListenLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            NamedPipeServerStream pipe = null;
            try
            {
                pipe = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    1,                              // one instance: single client at a time
                    PipeTransmissionMode.Message,    // MUST match client's ReadMode = Message
                    PipeOptions.Asynchronous);

                _pipeServer = pipe;

                await pipe.WaitForConnectionAsync(token);
                Connected?.Invoke();

                await ReadLoopAsync(pipe, token);
            }
            catch (OperationCanceledException)
            {
                break; // Stop() was called
            }
            catch (IOException)
            {
                // Client dropped mid-read/connect - fall through and re-create the pipe
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"NamedPipeServer error: {ex.Message}");
            }
            finally
            {
                Disconnected?.Invoke();
                try { pipe?.Dispose(); } catch { /* ignore */ }
                if (ReferenceEquals(_pipeServer, pipe)) _pipeServer = null;
            }
        }
    }

    private async Task ReadLoopAsync(NamedPipeServerStream pipe, CancellationToken token)
    {
        var buffer = new byte[4096];
        var messageBuilder = new StringBuilder();

        while (!token.IsCancellationRequested && pipe.IsConnected)
        {
            messageBuilder.Clear();
            int bytesRead;

            // In Message mode a single logical message can span multiple ReadAsync calls;
            // IsMessageComplete tells us when we actually have the whole thing.
            do
            {
                bytesRead = await pipe.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0)
                    return; // 0-byte read in message mode == client disconnected

                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            }
            while (!pipe.IsMessageComplete);

            string message = messageBuilder.ToString().Replace("\0", "").Trim();
            if (message.Length > 0)
                CommandReceived?.Invoke(message);
        }
    }

    /// <summary>Sends one message (e.g. "DOWN,5449868744") to the connected client, if any.</summary>
    public void SendMessage(string message)
    {
        if (_pipeServer?.IsConnected != true) return;

        byte[] data = Encoding.UTF8.GetBytes(message);
        try
        {
            // NamedPipeServerStream isn't guaranteed safe for concurrent writes from multiple
            // threads; serialize writes if you'll call SendMessage from more than one caller.
            lock (_writeLock)
            {
                _pipeServer.Write(data, 0, data.Length);
                _pipeServer.Flush();
            }
        }
        catch (IOException)
        {
            // Client dropped; the listen loop will notice on its next read and reset.
        }
    }

    public void Dispose() => Stop();
}
