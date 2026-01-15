using XMMTwrapper1.OcxInteropConstants;

internal static class XMMLvlContract
{
    // Properties
    public const int DISPID_Visible = 1;
    public const int DISPID_OnColor = 2;
    public const int DISPID_OffColor = 3;
    public const int DISPID_BackColor = 4;
    public const int DISPID_LineColor = 5;

    // Methods
    public const int DISPID_Draw = 6; // Draw(short sig, short sq)
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
