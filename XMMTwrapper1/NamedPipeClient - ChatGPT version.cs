using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

//Emulation of my original VB6 Named pipe for compatibility with TXLink server app.

public sealed class TxLinkPipeClientWin32 : IDisposable
{
    private const string PipePath = @"\\.\pipe\TXLinkCommandPipe";

    private SafeFileHandle? _handle;

    public bool IsConnected => _handle is { IsInvalid: false, IsClosed: false };

    public void Connect(uint timeoutMs = NMPWAIT_WAIT_FOREVER)
    {
        Close();

        if (!WaitNamedPipe(PipePath, timeoutMs))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "WaitNamedPipe failed.");

        _handle = CreateFile(
            PipePath,
            GENERIC_READ | GENERIC_WRITE,
            0,
            IntPtr.Zero,
            OPEN_EXISTING,
            0,
            IntPtr.Zero);

        if (_handle.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateFile on pipe failed.");

        // Set message read mode if server uses message mode:
        uint mode = PIPE_READMODE_MESSAGE;
        if (!SetNamedPipeHandleState(_handle, ref mode, IntPtr.Zero, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SetNamedPipeHandleState failed.");
    }

    public uint PeekAvailableBytes()
    {
        EnsureConnected();
        if (!PeekNamedPipe(_handle!, IntPtr.Zero, 0, out _, out uint totalAvail, out _))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "PeekNamedPipe failed.");
        return totalAvail;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        EnsureConnected();

        if (buffer is null) throw new ArgumentNullException(nameof(buffer));
        if ((uint)offset > buffer.Length || (uint)count > buffer.Length - offset) throw new ArgumentOutOfRangeException();

        // ReadFile wants a pinned buffer
        var gc = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
            if (!ReadFile(_handle!, ptr, (uint)count, out uint read, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "ReadFile failed.");
            return (int)read;
        }
        finally { gc.Free(); }
    }

    public void Write(byte[] buffer, int offset, int count)
    {
        EnsureConnected();

        var gc = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
            if (!WriteFile(_handle!, ptr, (uint)count, out uint written, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "WriteFile failed.");

            if (written != (uint)count)
                throw new IOException($"WriteFile wrote {written} of {count} bytes.");
        }
        finally { gc.Free(); }
    }

    public void WriteAscii(string s)
    {
        var bytes = Encoding.ASCII.GetBytes(s);
        Write(bytes, 0, bytes.Length);
    }

    public string ReadMessageUnicode(int maxBytes = 4096)
    {
        EnsureConnected();

        // Read raw bytes
        byte[] buf = new byte[maxBytes];
        int n = Read(buf, 0, buf.Length);
        if (n <= 0) return "";

        // Server writes UTF-16LE (Windows wchar_t). Decode only the bytes read.
        string s = System.Text.Encoding.Unicode.GetString(buf, 0, n);

        // Trim at first null terminator (server appends L'\0')
        int zero = s.IndexOf('\0');
        if (zero >= 0) s = s.Substring(0, zero);

        return s;
    }

    public void WriteMessageUnicode(string s)
    {
        EnsureConnected();

        // Server reads bytes and filters nulls on readMessage(),
        // so sending UTF-16LE keeps symmetry, and also VB6-like.
        // Add a terminating wide null (two zero bytes).
        byte[] payload = System.Text.Encoding.Unicode.GetBytes(s + "\0");
        Write(payload, 0, payload.Length);
    }


    public void Close()
    {
        _handle?.Dispose();
        _handle = null;
    }

    public void Dispose() => Close();

    private void EnsureConnected()
    {
        if (!IsConnected) throw new InvalidOperationException("Pipe is not connected.");
    }

    // ---- Constants (match your VB) ----
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint OPEN_EXISTING = 3;
    private const uint PIPE_READMODE_MESSAGE = 0x00000002;

    public const uint NMPWAIT_WAIT_FOREVER = 0xFFFFFFFF;
    public const uint NMPWAIT_USE_DEFAULT_WAIT = 0x00000000;
    public const uint NMPWAIT_NOWAIT = 0x00000001;

    // ---- P/Invoke ----
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(
        SafeFileHandle hFile,
        IntPtr lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(
        SafeFileHandle hFile,
        IntPtr lpBuffer,
        uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool PeekNamedPipe(
        SafeFileHandle hNamedPipe,
        IntPtr lpBuffer,
        uint nBufferSize,
        out uint lpBytesRead,
        out uint lpTotalBytesAvail,
        out uint lpBytesLeftThisMessage);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool WaitNamedPipe(string lpNamedPipeName, uint nTimeOut);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetNamedPipeHandleState(
        SafeFileHandle hNamedPipe,
        ref uint lpMode,
        IntPtr lpMaxCollectionCount,
        IntPtr lpCollectDataTimeout);
}
