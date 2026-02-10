using System.Reflection;
using System.Runtime.InteropServices.ComTypes;

//COM connection point host that links to OCX
//Events from OCX are handled in XmmrEventsSink.cs
//See Form1_Shown() to see how this is used
public sealed class XmmrRawEventHook : IDisposable
{
    //Handle to XMMR.OCX events
    private static readonly Guid IID_DXMMREvents =
        new("527A76C5-3C2D-4D68-99EC-B4D2F53CAE74");

    private readonly AxXMMTLib.AxXMMR _ax;

    private IConnectionPoint? _cp;
    private int _cookie;

    private readonly DxmmrEventsSink _sink = new();

    public XmmrRawEventHook(AxXMMTLib.AxXMMR ax)
    {
        _ax = ax;
    }

    // 
    public event Action<short[], short, short>? OnNotifyFftRaw
    {
        add => _sink.NotifyFftRaw += value;
        remove => _sink.NotifyFftRaw -= value;
    }

    public event Action<int[]>? OnNotifyGetXY
    {
        add => _sink.NotifyGetXY += value;
        remove => _sink.NotifyGetXY -= value;
    }

    public event Action<int[]>? OnNotifyNMMR
    {
        add => _sink.NotifyNMMR += value;
        remove => _sink.NotifyNMMR -= value;
    }

    //this flag is used to ensure FFT events do not overwhelm the Form processing
    // (actually do not think this is necessary because updates are quite slow ~ 100ms)
    public bool AcceptFft
    {
        get => _sink.AcceptFft;
        set => _sink.AcceptFft = value;
    }

    //start event handler
    public void Advise()
    {
        Log.LogThread("Advise entry");

        if (_cp != null) return;

        var ocx = GetUnderlyingOcx(_ax);
        var cpc = (IConnectionPointContainer)ocx;

        Guid iid = IID_DXMMREvents;

        Log.Mark($"[Hook] Advising IID={iid} sink={_sink.GetType().FullName}");

        cpc.FindConnectionPoint(ref iid, out _cp);
        _cp.Advise(_sink, out _cookie);

        Log.Mark($"[Hook] Advise OK cookie={_cookie}");
        GC.KeepAlive(_sink);
    }

    public void Unadvise()
    {
        if (_cp == null) return;
        try { _cp.Unadvise(_cookie); } catch { }
        _cookie = 0;
        _cp = null;
    }

    public void Dispose() => Unadvise();

    private static object GetUnderlyingOcx(AxHost ax)
    {
        if (!ax.IsHandleCreated)
            throw new InvalidOperationException("AxHost handle not created yet. Call Advise after handle creation / Shown+BeginInvoke.");

        // Prefer AxHost private Ocx property
        var hostType = typeof(AxHost);
        var prop = hostType.GetProperty("Ocx", BindingFlags.Instance | BindingFlags.NonPublic)
               ?? hostType.GetProperty("ocx", BindingFlags.Instance | BindingFlags.NonPublic);

        if (prop?.GetValue(ax) is object pv && pv != null) return pv;

        // Or GetOcx up the chain
        for (Type? t = ax.GetType(); t != null; t = t.BaseType)
        {
            var mi = t.GetMethod("GetOcx", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (mi != null)
            {
                var ocx = mi.Invoke(ax, null);
                if (ocx != null) return ocx;
            }
        }

        throw new InvalidOperationException("Could not locate underlying OCX instance on AxHost.");
    }
}
