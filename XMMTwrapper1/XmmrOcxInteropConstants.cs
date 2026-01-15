using System;
using System.Runtime.InteropServices;
using XMMTwrapper1.OcxInteropConstants;

namespace XMMTwrapper1.OcxInteropConstants
{
    internal static class XmmrOcxInteropConstants
    {
        // ---- VARTYPE (VARIANT.vt) ----
        public const ushort VT_EMPTY = 0x0000;
        public const ushort VT_I2 = 0x0002;
        public const ushort VT_I4 = 0x0003;
        public const ushort VT_R4 = 0x0004;
        public const ushort VT_R8 = 0x0005;
        public const ushort VT_BSTR = 0x0008;
        public const ushort VT_BOOL = 0x000B;
        public const ushort VT_VARIANT = 0x000C;
        public const ushort VT_UI1 = 0x0011;

        public const ushort VT_BYREF = 0x4000;

        public const ushort VT_BYREF_I2 = (ushort)(VT_BYREF | VT_I2); // 0x4002
        public const ushort VT_BYREF_I4 = (ushort)(VT_BYREF | VT_I4); // 0x4003

        // ---- IDispatch Invoke flags ----
        public const ushort DISPATCH_METHOD = 0x0001;
        public const ushort DISPATCH_PROPERTYGET = 0x0002;
        public const ushort DISPATCH_PROPERTYPUT = 0x0004;
        public const ushort DISPATCH_PROPERTYPUTREF = 0x0008;

        // ---- HRESULTs you’ll see a lot ----
        public const int S_OK = 0x00000000;
        public const int DISP_E_TYPEMISMATCH = unchecked((int)0x80020005);
        public const int DISP_E_BADPARAMCOUNT = unchecked((int)0x8002000E);

        // ---- Architecture note ----
        // In x86: C++ LONG == 32-bit signed == C# int
    }
}


internal static class XMMSpecContract
{
    // Properties
    public const int DISPID_BShowValue = 1;
    public const int DISPID_BShowLimiter = 2;
    public const int DISPID_Visible = 3;
    public const int DISPID_Notch1 = 4;
    public const int DISPID_Notch2 = 5;
    public const int DISPID_BaseFreq = 6;
    public const int DISPID_WidthFreq = 7;
    public const int DISPID_MarkFreq = 8;
    public const int DISPID_SpaceFreq = 9;
    public const int DISPID_BackColor = 10;
    public const int DISPID_MarkerColor = 11;
    public const int DISPID_SpectramColor = 12;
    public const int DISPID_LimiterColor = 13;

    // Methods
    public const int DISPID_Draw = 14; // Draw(short* pFFT, short size, short sampfreq)
    public const int DISPID_Clear = 15;
    public const int DISPID_UpdateByNMMT = 16; // UpdateByNMMT(long* pNMMT)
    public const int DISPID_UpdateByNMMR = 17; // UpdateByNMMR(long* pNMMR)

    // Event IDs
    public const int EVENT_OnLMouseDown = 1;
    public const int EVENT_OnLMouseMove = 2;
    public const int EVENT_OnLMouseUp = 3;
    public const int EVENT_OnRMouseDown = 4;
    public const int EVENT_OnRMouseUp = 5;
    public const int EVENT_OnRMouseMove = 6;

    // VARTYPE expectations
    // Draw(short* pFFT, short size, short sampfreq) => VTS_PI2, VTS_I2, VTS_I2
    public const ushort VT_Draw_pFFT = XmmrOcxInteropConstants.VT_BYREF_I2; // 0x4002
}

internal static class XMMWaterContract
{
    // Properties
    public const int DISPID_Visible = 1;
    public const int DISPID_BackColor = 2;
    public const int DISPID_SpectramColor = 3;
    public const int DISPID_BaseFreq = 4;
    public const int DISPID_WidthFreq = 5;

    // Methods
    public const int DISPID_Draw = 6; // Draw(short* pFFT, short size, short sampfreq)
    public const int DISPID_Clear = 7;

    // Event IDs
    public const int EVENT_OnLMouseDown = 1;
    public const int EVENT_OnLMouseUp = 2;
    public const int EVENT_OnLMouseMove = 3;
    public const int EVENT_OnRMouseDown = 4;
    public const int EVENT_OnRMouseMove = 5;
    public const int EVENT_OnRMouseUp = 6;

    // VARTYPE expectations
    public const ushort VT_Draw_pFFT = XmmrOcxInteropConstants.VT_BYREF_I2; // 0x4002
}

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


internal static class XMMLvlContract
{
    // Properties
    public const int DISPID_Visible = 1;
    public const int DISPID_OnColor = 2;
    public const int DISPID_OffColor = 3;
    public const int DISPID_BackColor = 4;
    public const int DISPID_LineColor = 5;

    // Methods
    public const int DISPID_Draw = 6;       // Draw(short sig, short sq)
    public const int DISPID_DrawByNMMT = 7; // DrawByNMMT(long* pNMMT)
    public const int DISPID_Clear = 8;
    public const int DISPID_DrawByNMMR = 9; // DrawByNMMR(long* pNMMR)

    // Event IDs
    public const int EVENT_OnLMouseDown = 1;
    public const int EVENT_OnLMouseUp = 2;
    public const int EVENT_OnLMouseMove = 3;
    public const int EVENT_OnRMouseDown = 4;
    public const int EVENT_OnRMouseUp = 5;
    public const int EVENT_OnRMouseMove = 6;
}

namespace Constants
{
    /// <summary>
    /// RXM_* message codes (APP -> MMTTY)
    /// Refactored from XMMRCtl.h enum.
    /// Numeric values MUST remain stable.
    /// </summary>
    internal enum RxmMessage : int
    {
        // --- Base commands ---
        Handle = 0x0000,        // RXM_HANDLE
        ReqHandle = 0x0001,     // RXM_REQHANDLE
        Exit = 0x0002,          // RXM_EXIT
        Ptt = 0x0003,           // RXM_PTT
        Char = 0x0004,          // RXM_CHAR

        // --- Window / view ---
        WinPos = 0x0005,        // RXM_WINPOS
        Width = 0x0006,         // RXM_WIDTH
        ReqPara = 0x0007,       // RXM_REQPARA
        SetBaud = 0x0008,       // RXM_SETBAUD
        SetMark = 0x0009,       // RXM_SETMARK

        SetSpace = 0x000A,      // RXM_SETSPACE
        SetSwitch = 0x000B,     // RXM_SETSWITCH
        SetHam = 0x000C,        // RXM_SETHAM
        ShowSetup = 0x000D,     // RXM_SHOWSETUP
        SetView = 0x000E,       // RXM_SETVIEW

        // --- DSP / filtering ---
        SetSqLevel = 0x000F,    // RXM_SETSQLVL
        Show = 0x0010,          // RXM_SHOW
        SetFig = 0x0011,        // RXM_SETFIG
        SetReso = 0x0012,       // RXM_SETRESO
        SetLpf = 0x0013,        // RXM_SETLPF

        // --- Runtime / control ---
        SetTxDelay = 0x0014,    // RXM_SETTXDELAY
        UpdateCom = 0x0015,     // RXM_UPDATECOM
        Suspend = 0x0016,       // RXM_SUSPEND
        Notch = 0x0017,         // RXM_NOTCH
        Profile = 0x0018,       // RXM_PROFILE

        // --- Later additions ---
        Timer = 0x0019,         // RXM_TIMER
        EnableFocus = 0x001A,   // RXM_ENBFOCUS (Ver 1.63A)
        SetDefFreq = 0x001B,    // RXM_SETDEFFREQ (Ver 1.63B)
        SetLength = 0x001C,     // RXM_SETLENGTH (Ver 1.63B)
        EnableShared = 0x001D, // RXM_ENBSHARED (Ver 1.64)

        // --- FSK ---
        PttFsk = 0x001E, // RXM_PTTFSK (Ver 1.67B)
    }
}



namespace NmmrContract
{
    /// <summary>
    /// NMMR structure (MMTTY → APP).
    /// Layout must match the native definition exactly.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Nmmr
    {
        public uint MarkFreq;     // DWORD m_markfreq
        public uint SpaceFreq;    // DWORD m_spacefreq
        public uint SigLevel;     // DWORD m_siglevel
        public uint SqLevel;      // DWORD m_sqlevel
        public uint CodeSwitch;   // DWORD m_codeswitch
        public uint CodeView;     // DWORD m_codeview
        public uint Notch1;       // DWORD m_notch1
        public uint Notch2;       // DWORD m_notch2
        public uint Baud;         // DWORD m_baud
        public uint Fig;          // DWORD m_fig
        public uint RadioFreq;    // DWORD m_radiofreq

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 53)]
        public uint[] Reserved;   // DWORD m_Reserved[53]
    }
}


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
    public const ushort VT_OnNotifyFFT_pFFT = XmmrOcxInteropConstants.VT_BYREF_I2; // 0x4002
    public const ushort VT_OnNotifyFFT_size = XmmrOcxInteropConstants.VT_I2;
    public const ushort VT_OnNotifyFFT_sampfreq = XmmrOcxInteropConstants.VT_I2;

    // FireOnNotifyXY(long FAR* pXY)
    public const ushort VT_OnNotifyXY_pXY = XmmrOcxInteropConstants.VT_BYREF_I4; // 0x4003

    // FireOnNotifyNMMR(long FAR* pNMMR)
    public const ushort VT_OnNotifyNMMR_pNMMR = XmmrOcxInteropConstants.VT_BYREF_I4; // 0x4003

    // ---- Buffer sizes implied by the header ----
    public const int FFT_Count = 2048;      // short[2048] (m_fft) in the ctrl
    public const int XY_Count = 1024;       // int[1024]  (m_xy) in the ctrl
    public const int NMMR_Count = 64;       // int[64]  NMMR status structure
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

namespace MmttySwitchFlagsConstants
{
    // See CodeSwitch Settings.md for full functional descriptions
    [Flags]
    internal enum MmttySwitchFlags : uint
    {
        None = 0,
        F1 = 0x00000001,    // NOTE bits 1 and 2 define 3 different decode filters
        F2 = 0x00000002,
        AFC = 0x00000004,   // b2
        NET = 0x00000008,   // b3
        ATC = 0x00000010,   // b4
        BPF = 0x00000020,   // b5
        LMS = 0x00000040,   // b6 LMS/Notch
        SQ  = 0x00000080,   // b7
        REV = 0x00000100,   // b8
        UOS = 0x00000200,   // b9
        NOT = 0x00002000,   // b13 Notch/LMS
        TWO = 0x00004000,   // b14
    }
}



