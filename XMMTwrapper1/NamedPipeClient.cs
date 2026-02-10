using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;

public class NamedPipeClient : IDisposable
{
    private NamedPipeClientStream _pipeClient;
    private readonly string _pipeName;

    // P/Invoke required to check pipe status without blocking the STA thread
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool PeekNamedPipe(IntPtr handle, byte[] buffer, uint nBufferSize,
        IntPtr bytesRead, out uint bytesAvail, IntPtr bytesLeftThisMessage);

    public NamedPipeClient(string pipeName = "TXLinkCommandPipe")
    {
        _pipeName = pipeName;
    }

    public bool TryConnect()
    {
        if (_pipeClient != null && _pipeClient.IsConnected) return true;

        try
        {
            _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut);
            // Connect with a very short timeout so the UI doesn't stutter
            _pipeClient.Connect(10);
            _pipeClient.ReadMode = PipeTransmissionMode.Message;
            return true;
        }
        catch { return false; }
    }

    public string PollForMessage()
    {
        if (_pipeClient == null || !_pipeClient.IsConnected) return null;

        // Check if there is a message waiting (Non-blocking)
        if (PeekNamedPipe(_pipeClient.SafePipeHandle.DangerousGetHandle(), null, 0, IntPtr.Zero, out uint bytesAvail, IntPtr.Zero))
        {
            if (bytesAvail > 0)
            {
                byte[] buffer = new byte[bytesAvail];
                int read = _pipeClient.Read(buffer, 0, buffer.Length);

                // The C++ server writes Wide Characters (UTF-16)
                string msg = Encoding.Unicode.GetString(buffer, 0, read);
                return msg.TrimEnd('\0'); // Remove the L'\0' appended by C++
            }
        }
        return null;
    }

    public void SendCommand(string cmd)
    {
        if (_pipeClient?.IsConnected == true)
        {
            // The C++ server readMessage filters out nulls and treats data as chars
            byte[] buffer = Encoding.ASCII.GetBytes(cmd);
            _pipeClient.Write(buffer, 0, buffer.Length);
        }
    }

    public void Dispose() => _pipeClient?.Dispose();
}