using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Broadcasts DOWN/UP keying timing events over UDP multicast on the loopback
/// interface. Any number of independent listener processes on this machine can
/// join the same group and each receives its own copy of every message - no
/// pipe instances, no registration, no coordination needed.
///
/// Usage:
///   var sender = new UdpTimingSender();
///   sender.SendKeyEvent("DOWN", 5449868744);
///   sender.SendKeyEvent("UP",   5449917244);
/// </summary>
public class UdpTimingSender : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _multicastEndpoint;

    /// <param name="multicastAddress">
    /// Must be in the 224.0.0.0-239.255.255.255 range. 239.255.x.x is part of the
    /// "administratively scoped" (site-local) range, a safe default for
    /// machine-local use.
    /// </param>
    /// <param name="port">UDP port for the group.</param>
    /// <param name="ttl">
    /// Multicast TTL. 1 = stays on the local subnet (won't cross a router) - the
    /// safe default. Set to 0 if you want to guarantee it never leaves this
    /// machine even on a bridged/virtual adapter setup.
    /// </param>
    public UdpTimingSender(string multicastAddress = "239.255.42.99", int port = 6789, int ttl = 1)
    {
        _multicastEndpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), port);

        _udpClient = new UdpClient();
        _udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, ttl);

        // Ensures other processes on THIS machine receive the datagrams too
        // (true is the .NET default, set explicitly here for clarity).
        _udpClient.MulticastLoopback = true;
    }

    /// <summary>Sends one "STATE,microseconds" line, e.g. "DOWN,5449868744".</summary>
    public void SendKeyEvent(string state, long microseconds)
    {
        SendRaw($"{state},{microseconds}");
    }

    public void SendRaw(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        try
        {
            _udpClient.Send(data, data.Length, _multicastEndpoint);
        }
        catch (SocketException)
        {
            // Best-effort telemetry - no listeners, transient network hiccup, etc.
            // Not worth throwing over a dropped timing datagram.
        }
    }

    public void Dispose() => _udpClient?.Dispose();
}
