using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;



public class NamedPipeClient : IDisposable
{
    private NamedPipeClientStream _pipeClient;
    private readonly string _pipeName;

    [DllImport("kernel32.dll", EntryPoint = "PeekNamedPipe", SetLastError = true)]
    private static extern bool PeekNamedPipe(IntPtr hHandle, byte[] buf, uint nBuf, IntPtr bytesRead, out uint avail, IntPtr left);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetHandleInformation(IntPtr hObject, out uint lpdwFlags);

    public NamedPipeClient(string pipeName = "TXLinkCommandPipe")
    {
        _pipeName = pipeName;
    }

    public bool TryConnect()
    {
        if (_pipeClient != null && _pipeClient.IsConnected) return true;

        try
        {
            if (_pipeClient != null) { _pipeClient.Dispose(); _pipeClient = null; }

            // Match the server's Duplex (InOut) access
            _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            _pipeClient.Connect(10);

            // This MUST match the server's PIPE_TYPE_MESSAGE
            _pipeClient.ReadMode = PipeTransmissionMode.Message;

            return true;
        }
        catch (Exception)
        {
            _pipeClient?.Dispose();
            _pipeClient = null;
            return false;
        }
    }

    public string PollForMessage()
    {
        // If we have no object, we are already "disconnected"
        if (_pipeClient == null) return string.Empty;

        IntPtr handle = _pipeClient.SafePipeHandle.DangerousGetHandle();

        // 1. Check if the handle is still valid at the OS level
        if (handle == IntPtr.Zero || !GetHandleInformation(handle, out _))
        {
            CloseAndNullPipe();
            return string.Empty;
        }

        try
        {
            uint totalBytesAvail;
            // 2. Peek is the most reliable "Are you still there?" check.
            // If the server closed its end, PeekNamedPipe will return false.
            bool success = PeekNamedPipe(handle, null, 0, IntPtr.Zero, out totalBytesAvail, IntPtr.Zero);

            if (!success)
            {
                // The pipe is broken or the server disconnected
                CloseAndNullPipe();
                return string.Empty;
            }

            // 3. Read if data is waiting
            if (totalBytesAvail > 0)
            {
                byte[] buffer = new byte[totalBytesAvail];
                int actualRead = _pipeClient.Read(buffer, 0, buffer.Length);
                if (actualRead > 0)
                {
                    return System.Text.Encoding.UTF8.GetString(buffer, 0, actualRead).Replace("\0", "").Trim();
                }
            }
        }
        catch (Exception)
        {
            CloseAndNullPipe();
        }

        return string.Empty;
    }

    private string CleanFrequencyData(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // Optional: Strip null terminators or whitespace often sent by C++ servers
        return input.Trim().Replace("\0", "");
    }

    private void CloseAndNullPipe()
    {
        _pipeClient?.Dispose();
        _pipeClient = null; // This ensures TryConnect() runs its logic next time
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