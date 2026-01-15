using XMMTwrapper1.OcxInteropConstants;

internal static class XmmXyContract
{
    // Interface method DISPIDs (from the header)
    public const int DISPID_Draw = 7;
    public const int DISPID_Clear = 8;

    // Event IDs (from the header)
    public const int EVENT_OnLButtonClick = 1;
    public const int EVENT_OnRButtonClick = 2;

    // XY buffer layout contract (from DrawXY implementation)
    public const int XY_CountTotal = 1024;  // LONG[1024]
    public const int XY_CountHalf = 512;   // X[512], Y[512]

    // VARTYPE expectations for Draw(long* pXY)
    public const ushort VT_I4 = 0x0003;
    public const ushort VT_BYREF = 0x4000;
    public const ushort VT_BYREF_I4 = VT_BYREF | VT_I4; // 0x4003

    // NOTE: x86: C++ LONG == 32-bit == C# int
}
