using System;
using System.Collections.Generic;
using System.IO.Ports;              //*** use NuGet to install this
using System.Text;

public class SerialMessageHandler : IDisposable
{
    private SerialPort _serialPort;
    private StringBuilder _buffer = new StringBuilder();
    private Queue<string> _messageQueue = new Queue<string>();

    public bool IsOpen => _serialPort?.IsOpen ?? false;

    public void Connect(string portName, int baudRate = 115200)
    {
        if (_serialPort != null && _serialPort.IsOpen) _serialPort.Close();

        _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        _serialPort.Encoding = Encoding.ASCII;
        _serialPort.Open();
    }

    /// <summary>
    /// Looks at the next character without removing it from the buffer.
    /// Returns "" if no data is available.
    /// </summary>
    public string PeekChar()
    {
        FetchHardwareData();

        if (_buffer.Length > 0)
        {
            return _buffer[0].ToString();
        }
        return "";
    }

    /// <summary>
    /// Reads one character and removes it from the buffer.
    /// Returns "" if no data is available.
    /// </summary>
    public string ReadChar()
    {
        FetchHardwareData();

        if (_buffer.Length > 0)
        {
            char c = _buffer[0];
            _buffer.Remove(0, 1);
            return c.ToString();
        }
        return "";
    }

    /// <summary>
    /// Call this in your Timer_Tick to process packets.
    /// </summary>
    public void Poll()
    {
        FetchHardwareData();
        ParseBuffer();
    }

    private void FetchHardwareData()
    {
        if (!IsOpen || _serialPort.BytesToRead == 0) return;

        // Pull raw data into our managed buffer
        string rawData = _serialPort.ReadExisting();
        _buffer.Append(rawData);
    }

    private void ParseBuffer()
    {
        string content = _buffer.ToString();
        int startIdx;

        while ((startIdx = content.IndexOf('{')) != -1)
        {
            int endIdx = content.IndexOf('}', startIdx);
            if (endIdx != -1)
            {
                string message = content.Substring(startIdx + 1, endIdx - startIdx - 1);
                _messageQueue.Enqueue(message);
                content = content.Substring(endIdx + 1);
            }
            else break;
        }

        _buffer.Clear();
        _buffer.Append(content);
    }

    public bool HasMessages => _messageQueue.Count > 0;
    public string GetNextMessage() => _messageQueue.Count > 0 ? _messageQueue.Dequeue() : null;

    public void SendDelimMessage(string msg)
    {
        if (IsOpen) _serialPort.Write($"{{{msg}}}");
    }

    public void SendMessage(string msg)
    {
        if (IsOpen) _serialPort.Write(msg);
    }

    public void Dispose() => _serialPort?.Dispose();
}