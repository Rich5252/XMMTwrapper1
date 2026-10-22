// JttyLink.cs
//
// TCP server for WS's MMTTY-style text link (the one written for N1MM).
//
// WS is started with  --mode jtty -p <port>  and connects as a CLIENT to 127.0.0.1:<port>.
// This class is the server end. It is used for RECEIVE ONLY: transmit goes through TXLink
// over the named pipe, so we never send <TXTEXT> (that would make WS transmit through its
// own sound card output).
//
// Wire format, both directions:   <TAG>           e.g. <OUTPUTCOMPLETE>
//                                 <TAG:len>data   e.g. <RXTEXT:5>G4AHN
// WS sends decoded text as <RXTEXT:len>text as each frame decodes, with "\r\n" before
// each new message.
//
// Events are raised on the SynchronizationContext captured in Start() - call Start() from
// the UI thread and handlers can touch controls directly without BeginInvoke.

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace XMMTwrapper1
{
    public sealed class JttyLink : IDisposable
    {
        // Decoded text from WS (may be a fragment of a message, may contain \r\n)
        public event Action<string>? RxText;

        // Any other tag from WS, e.g. ("OUTPUTCOMPLETE", ""). Data is "" for tags without a length.
        public event Action<string, string>? TagReceived;

        // true when WS connects, false when it disconnects
        public event Action<bool>? ConnectionChanged;

        public bool IsRunning => _listener != null;
        public bool IsConnected => _client?.Connected == true;
        public int Port { get; private set; }

        // MMTTY protocol is plain 8-bit text; Latin1 maps bytes 1:1 so <TAG:len> lengths
        // (which count bytes) stay correct even if a stray non-ASCII byte turns up.
        private static readonly Encoding Wire = Encoding.Latin1;

        private const int MaxTagLength = 32;          // longest tag name we will accept
        private const int MaxDataLength = 64 * 1024;  // sanity limit on <TAG:len>

        private TcpListener? _listener;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private SynchronizationContext? _sync;
        private readonly object _sendLock = new();

        // Start listening on 127.0.0.1:port. Safe to call again with a new port.
        public void Start(int port)
        {
            Stop();

            _sync = SynchronizationContext.Current;     // UI thread if called from the form
            Port = port;
            _cts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            Log.Mark($"JttyLink listening on 127.0.0.1:{port}");

            _ = AcceptLoopAsync(_listener, _cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();

            try { _listener?.Stop(); } catch { }
            _listener = null;

            DropClient(raiseEvent: true);

            _cts?.Dispose();
            _cts = null;
        }

        // Send a tag to WS, e.g. SendTag("ABORT") or SendTag("XMIT", "OFF").
        // Not needed for receive-only use, but here for completeness.
        // Returns false if WS is not connected.
        public bool SendTag(string tag, string? data = null)
        {
            string msg = data == null ? $"<{tag}>" : $"<{tag}:{Wire.GetByteCount(data)}>{data}";
            byte[] bytes = Wire.GetBytes(msg);

            lock (_sendLock)
            {
                var s = _stream;
                if (s == null) return false;
                try
                {
                    s.Write(bytes, 0, bytes.Length);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Mark($"JttyLink send failed: {ex.Message}");
                    return false;
                }
            }
        }

        public void Dispose() => Stop();

        // ---------------------------------------------------------------------

        private async Task AcceptLoopAsync(TcpListener listener, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (SocketException ex)
                {
                    Log.Mark($"JttyLink accept error: {ex.Message}");
                    await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);
                    continue;
                }

                // Only one WS instance at a time; a new connection (e.g. WS restarted)
                // replaces the old one.
                DropClient(raiseEvent: false);

                client.NoDelay = true;
                _client = client;
                _stream = client.GetStream();
                Post(() => ConnectionChanged?.Invoke(true));
                Log.Mark("JttyLink: WS connected");

                await ReadLoopAsync(client, _stream, ct).ConfigureAwait(false);

                // ReadLoop returned: remote closed, error, or Stop()
                if (ReferenceEquals(_client, client))
                {
                    DropClient(raiseEvent: true);
                    Log.Mark("JttyLink: WS disconnected");
                }
            }
        }

        private async Task ReadLoopAsync(TcpClient client, NetworkStream stream, CancellationToken ct)
        {
            var parser = new MmttyFrameParser();
            var buf = new byte[4096];

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int n = await stream.ReadAsync(buf.AsMemory(0, buf.Length), ct).ConfigureAwait(false);
                    if (n <= 0) break;                  // remote closed

                    parser.Append(buf, n);
                    while (parser.TryGetNext(out string tag, out string data))
                    {
                        Dispatch(tag, data);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
        }

        private void Dispatch(string tag, string data)
        {
            if (tag.Equals("RXTEXT", StringComparison.OrdinalIgnoreCase))
            {
                if (data.Length > 0) Post(() => RxText?.Invoke(data));
            }
            else
            {
                Post(() => TagReceived?.Invoke(tag.ToUpperInvariant(), data));
            }
        }

        private void DropClient(bool raiseEvent)
        {
            bool wasConnected = _client != null;
            lock (_sendLock)
            {
                try { _stream?.Dispose(); } catch { }
                try { _client?.Dispose(); } catch { }
                _stream = null;
                _client = null;
            }
            if (raiseEvent && wasConnected) Post(() => ConnectionChanged?.Invoke(false));
        }

        private void Post(Action a)
        {
            if (_sync != null) _sync.Post(_ => a(), null);
            else a();
        }

        // ---------------------------------------------------------------------
        // Stream parser for <TAG> and <TAG:len>data. TCP may split or join frames
        // anywhere, so bytes are accumulated until a whole frame is present.
        // Bytes outside a frame are skipped.

        private sealed class MmttyFrameParser
        {
            private readonly List<byte> _buf = new();

            public void Append(byte[] data, int count)
            {
                for (int i = 0; i < count; i++) _buf.Add(data[i]);
            }

            public bool TryGetNext(out string tag, out string data)
            {
                tag = data = "";

                while (true)
                {
                    // Discard anything before the next '<'
                    int lt = _buf.IndexOf((byte)'<');
                    if (lt < 0) { _buf.Clear(); return false; }
                    if (lt > 0) _buf.RemoveRange(0, lt);

                    int gt = _buf.IndexOf((byte)'>');
                    if (gt < 0)
                    {
                        // Incomplete header. If it is already too long it is not a tag.
                        if (_buf.Count > MaxTagLength + 12) { _buf.RemoveAt(0); continue; }
                        return false;
                    }

                    string header = Wire.GetString(_buf.GetRange(1, gt - 1).ToArray());
                    int colon = header.IndexOf(':');
                    string name = colon < 0 ? header : header.Substring(0, colon);

                    if (!IsValidTagName(name))
                    {
                        _buf.RemoveAt(0);       // stray '<', resync on the next one
                        continue;
                    }

                    if (colon < 0)
                    {
                        _buf.RemoveRange(0, gt + 1);
                        tag = name;
                        return true;
                    }

                    if (!int.TryParse(header.AsSpan(colon + 1), out int len) || len < 0 || len > MaxDataLength)
                    {
                        _buf.RemoveAt(0);
                        continue;
                    }

                    if (_buf.Count < gt + 1 + len) return false;   // wait for the rest

                    tag = name;
                    data = Wire.GetString(_buf.GetRange(gt + 1, len).ToArray());
                    _buf.RemoveRange(0, gt + 1 + len);
                    return true;
                }
            }

            private static bool IsValidTagName(string s)
            {
                if (s.Length == 0 || s.Length > MaxTagLength) return false;
                foreach (char c in s)
                    if (!char.IsLetterOrDigit(c) && c != '_') return false;
                return true;
            }
        }
    }
}
