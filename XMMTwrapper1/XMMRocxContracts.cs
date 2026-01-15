using System.Runtime.InteropServices;
using XMMTwrapper1.OcxInteropConstants;

internal static class XmmrContract
{
    // ---- Event IDs (from header enum) ----
    public const int EVENT_OnConnected = 1;
    public const int EVENT_OnCharRcvd = 2;
    public const int EVENT_OnPttEvent = 3;
    public const int EVENT_OnDisconnected = 4;
    public const int EVENT_OnNotifyNMMR = 5;
    public const int EVENT_OnNotchChanged = 6;
    public const int EVENT_OnViewChanged = 7;
    public const int EVENT_OnSwitchChanged = 8;
    public const int EVENT_OnFreqChanged = 9;
    public const int EVENT_OnBaudChanged = 10;
    public const int EVENT_OnNotifyFFT = 11; // VTS_PI2, I2, I2
    public const int EVENT_OnNotifyXY = 12; // VTS_PI4
    public const int EVENT_OnRadioFreqChanged = 13;
    public const int EVENT_OnRadioNameChanged = 14;
    public const int EVENT_OnComNameChanged = 15;
    public const int EVENT_OnFigChanged = 16;
    public const int EVENT_OnTranslateMessage = 17;

    // ---- Key DISPIDs (subset; add more if you want) ----
    public const int DISPID_BNotifyXY = 7;
    public const int DISPID_BNotifyFFT = 8;

    public const int DISPID_SmpFreq = 16;
    public const int DISPID_SmpFFT = 17;
    public const int DISPID_SetSmpFFT = 17; // property put
    public const int DISPID_SmpDemFreq = 22;

    public const int DISPID_PostMmttyMessage = 28;
    public const int DISPID_SetMmttySwitch = 29;
    public const int DISPID_SetMmttyView = 30;
    public const int DISPID_SetMmttyPTT = 31;
    public const int DISPID_SendChar = 32;
    public const int DISPID_SendString = 33;
    public const int DISPID_ReadNMMR = 34;

    // ---- Event argument VARIANT expectations ----
    // FireOnNotifyFFT(short FAR* pFFT, short size, short sampfreq)
    public const ushort VT_OnNotifyFFT_pFFT = OcxInteropConstants.VT_BYREF_I2; // 0x4002
    public const ushort VT_OnNotifyFFT_size = OcxInteropConstants.VT_I2;
    public const ushort VT_OnNotifyFFT_sampfreq = OcxInteropConstants.VT_I2;

    // FireOnNotifyXY(long FAR* pXY)
    public const ushort VT_OnNotifyXY_pXY = OcxInteropConstants.VT_BYREF_I4; // 0x4003

    // FireOnNotifyNMMR(long FAR* pNMMR)
    public const ushort VT_OnNotifyNMMR_pNMMR = OcxInteropConstants.VT_BYREF_I4; // 0x4003

    // ---- Buffer sizes implied by the header ----
    public const int FFT_Count = 2048; // short[2048] (m_fft) in the ctrl
    public const int XY_Count = 1024; // long[1024]  (m_xy) in the ctrl
}

//Following to document data types from the original code
//However need to take special care of dispatch types due to VB6
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
internal unsafe struct XmmrComArray
{
    public int smpFreq;                           // LONG
    public fixed sbyte title[128];                // CHAR[128]
    public fixed sbyte comName[16];               // CHAR[16]
    public int smpFFT;                            // LONG

    public int flagFFT;                           // LONG
    public fixed int arrayFFT[2048];              // LONG[2048]
    public int flagXY;                            // LONG
    public fixed int arrayX[512];                 // LONG[512]
    public fixed int arrayY[512];                 // LONG[512]

    public fixed sbyte verMMTTY[16];              // CHAR[16]
    public fixed sbyte comRadio[16];              // CHAR[16]
    public int LostSound;                         // LONG
    public int OverFlow;                          // LONG
    public int ErrorClock;                        // LONG (ppm)
    public int smpDemFreq;                        // LONG
    public int TxBuffCount;                       // LONG
    public fixed sbyte ProfileName[16 * 64];      // CHAR[16][64] flattened
    public fixed int dummy[2048];                 // LONG[2048]
}

