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
        Handle = 0x0000, // RXM_HANDLE
        ReqHandle = 0x0001, // RXM_REQHANDLE
        Exit = 0x0002, // RXM_EXIT
        Ptt = 0x0003, // RXM_PTT
        Char = 0x0004, // RXM_CHAR

        // --- Window / view ---
        WinPos = 0x0005, // RXM_WINPOS
        Width = 0x0006, // RXM_WIDTH
        ReqPara = 0x0007, // RXM_REQPARA
        SetBaud = 0x0008, // RXM_SETBAUD
        SetMark = 0x0009, // RXM_SETMARK

        SetSpace = 0x000A, // RXM_SETSPACE
        SetSwitch = 0x000B, // RXM_SETSWITCH
        SetHam = 0x000C, // RXM_SETHAM
        ShowSetup = 0x000D, // RXM_SHOWSETUP
        SetView = 0x000E, // RXM_SETVIEW

        // --- DSP / filtering ---
        SetSqLevel = 0x000F, // RXM_SETSQLVL
        Show = 0x0010, // RXM_SHOW
        SetFig = 0x0011, // RXM_SETFIG
        SetReso = 0x0012, // RXM_SETRESO
        SetLpf = 0x0013, // RXM_SETLPF

        // --- Runtime / control ---
        SetTxDelay = 0x0014, // RXM_SETTXDELAY
        UpdateCom = 0x0015, // RXM_UPDATECOM
        Suspend = 0x0016, // RXM_SUSPEND
        Notch = 0x0017, // RXM_NOTCH
        Profile = 0x0018, // RXM_PROFILE

        // --- Later additions ---
        Timer = 0x0019, // RXM_TIMER
        EnableFocus = 0x001A, // RXM_ENBFOCUS (Ver 1.63A)
        SetDefFreq = 0x001B, // RXM_SETDEFFREQ (Ver 1.63B)
        SetLength = 0x001C, // RXM_SETLENGTH (Ver 1.63B)
        EnableShared = 0x001D, // RXM_ENBSHARED (Ver 1.64)

        // --- FSK ---
        PttFsk = 0x001E, // RXM_PTTFSK (Ver 1.67B)
    }
}
