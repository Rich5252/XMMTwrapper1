using XMMTwrapper1.OcxInteropConstants;

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
    public const ushort VT_Draw_pFFT = OcxInteropConstants.VT_BYREF_I2; // 0x4002
}
