// This file contains the form resize handling.
//
// Layout rules when the window is resized:
//   panelButtons       stays bottom-left (moves down/up with the bottom edge)
//   panelLogging       stays on the right edge (moves left/right with the right edge)
//   panelTextReceived  fills the space between them; tbTextReceived fills its panel
//
// The margins are captured from the designer layout in Form1_Load (before the saved
// window size is restored) so they are right at any DPI scaling.

namespace XMMTwrapper1
{
    //[System.ComponentModel.DesignerCategory("Code")]
    public partial class Form1
    {

        private bool _layoutCaptured = false;

        // form edge -> panel
        private int _marginButtonsBottom;       // bottom of panelButtons to bottom of client area (includes status strip)
        private int _marginLoggingRight;        // right of panelLogging to right of client area

        // panel -> panel
        private int _gapTextToLogging;          // panelLogging.Left - panelTextReceived.Right (can be slightly negative)
        private int _gapTextToButtons;          // panelButtons.Top - panelTextReceived.Bottom

        // inside panelTextReceived
        private int _tbRightMargin;             // panel client width  - tbTextReceived.Right
        private int _tbBottomMargin;            // panel client height - tbTextReceived.Bottom
        private int _clearBtnRightMargin;       // panel client width  - btnClearText.Right

        // Call once from Form1_Load, BEFORE the saved Left/Top/Width/Height are applied
        private void CaptureResizeLayout()
        {
            _marginButtonsBottom = ClientSize.Height - panelButtons.Bottom;
            _marginLoggingRight = ClientSize.Width - panelLogging.Right;

            _gapTextToLogging = panelLogging.Left - panelTextReceived.Right;
            _gapTextToButtons = panelButtons.Top - panelTextReceived.Bottom;

            _tbRightMargin = panelTextReceived.ClientSize.Width - tbTextReceived.Right;
            _tbBottomMargin = panelTextReceived.ClientSize.Height - tbTextReceived.Bottom;
            _clearBtnRightMargin = panelTextReceived.ClientSize.Width - btnClearText.Right;

            // The designed layout is the smallest the panels can be laid out at
            MinimumSize = Size;

            _layoutCaptured = true;
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            if (!_layoutCaptured || WindowState == FormWindowState.Minimized) return;

            SuspendLayout();

            // Buttons: bottom-left
            panelButtons.Top = ClientSize.Height - _marginButtonsBottom - panelButtons.Height;

            // Logging: right edge
            panelLogging.Left = ClientSize.Width - _marginLoggingRight - panelLogging.Width;
            panelLogging2.Left = panelLogging.Right - 253;
            panelLogging2.Top = panelLogging.Bottom;

            // Optional: keep the XY scope above it on the right edge as well
            // axxmmxy1.Left = ClientSize.Width - _marginLoggingRight - axxmmxy1.Width;

            // Text panel: fill the space between the left edge, logging panel and button panel
            int w = panelLogging.Left - _gapTextToLogging - panelTextReceived.Left;
            int h = panelButtons.Top - _gapTextToButtons - panelTextReceived.Top;
            panelTextReceived.Size = new Size(Math.Max(w, 100), Math.Max(h, 60));

            // ... and its contents
            Size inner = panelTextReceived.ClientSize;
            tbTextReceived.Size = new Size(
                Math.Max(inner.Width - _tbRightMargin - tbTextReceived.Left, 50),
                Math.Max(inner.Height - _tbBottomMargin - tbTextReceived.Top, 30));
            RxTextScroller.Size = new Size(
                Math.Max(inner.Width - _tbRightMargin - RxTextScroller.Left, 50),
                Math.Max(inner.Height - _tbBottomMargin - RxTextScroller.Top, 30));
            btnClearText.Left = inner.Width - _clearBtnRightMargin - btnClearText.Width;

            ResumeLayout(true);
        }
    }
}
