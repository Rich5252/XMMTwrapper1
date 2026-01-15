using XMMTwrapper1.OcxInteropConstants;

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
    public const ushort VT_Draw_pFFT = OcxInteropConstants.VT_BYREF_I2; // 0x4002
}
