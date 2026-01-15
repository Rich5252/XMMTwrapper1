using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;


//This links to my TX (C++) application which runs in my SDRUno plugin project (TXLink)
// 
public sealed class TxLinkPipeClient : IDisposable
{
    // VB: "\\.\pipe\TXLinkCommandPipe"
    private const string PipeName = "TXLinkCommandPipe";

    private NamedPipeClientStream? _pipe;

    public bool IsConnected => _pipe?.IsConnected == true;

    public void Connect(int timeoutMs = Timeout.Infinite)
    {
        _pipe?.Dispose();
        _pipe = new NamedPipeClientStream(
            serverName: ".",
            pipeName: PipeName,
            direction: PipeDirection.InOut,
            options: PipeOptions.Asynchronous);

        _pipe.Connect(timeoutMs);

        // Match VB / server: PIPE_READMODE_MESSAGE
        SetReadModeMessage(_pipe);
    }

    public void WriteString(string s)
    {
        if (_pipe is null || !_pipe.IsConnected) throw new InvalidOperationException("Not connected.");
        var bytes = Encoding.ASCII.GetBytes(s);
        _pipe.Write(bytes, 0, bytes.Length);
        _pipe.Flush();
    }

    // Reads one message (works best with message-mode server)
    public string ReadMessageAscii()
    {
        if (_pipe is null || !_pipe.IsConnected) throw new InvalidOperationException("Not connected.");

        var buffer = new byte[4096];
        using var ms = new MemoryStream();

        do
        {
            int n = _pipe.Read(buffer, 0, buffer.Length);
            if (n <= 0) break;
            ms.Write(buffer, 0, n);
        }
        while (!_pipe.IsMessageComplete);

        return Encoding.ASCII.GetString(ms.ToArray());
    }

    public void Dispose()
    {
        _pipe?.Dispose();
        _pipe = null;
    }

    // ---- P/Invoke to set PIPE_READMODE_MESSAGE on the underlying handle ----
    private const uint PIPE_READMODE_MESSAGE = 0x00000002;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetNamedPipeHandleState(
        IntPtr hNamedPipe,
        ref uint lpMode,
        IntPtr lpMaxCollectionCount,
        IntPtr lpCollectDataTimeout);

    private static void SetReadModeMessage(NamedPipeClientStream pipe)
    {
        uint mode = PIPE_READMODE_MESSAGE;
        // SafePipeHandle has a DangerousGetHandle; OK here because stream is alive.
        IntPtr h = pipe.SafePipeHandle.DangerousGetHandle();

        if (!SetNamedPipeHandleState(h, ref mode, IntPtr.Zero, IntPtr.Zero))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),
                "Failed to set pipe read mode to PIPE_READMODE_MESSAGE.");
        }
    }
}
