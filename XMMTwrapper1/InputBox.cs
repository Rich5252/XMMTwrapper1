public static class InputBox
{
    public static string Show(string prompt, string title, string defaultValue = "")
    {
        // Create the form
        Form promptForm = new Form()
        {
            Width = 400,
            Height = 180,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            AcceptButton = null,
            CancelButton = null
        };

        // Create the Prompt Label
        Label lblPrompt = new Label() { Left = 20, Top = 20, Width = 350, Text = prompt, AutoSize = true };

        // Create the TextBox
        TextBox txtInput = new TextBox() { Left = 20, Top = 50, Width = 340, Text = defaultValue };

        // Create OK Button
        Button btnOk = new Button() { Text = "OK", Left = 170, Width = 90, Top = 90, DialogResult = DialogResult.OK };

        // Create Cancel Button
        Button btnCancel = new Button() { Text = "Cancel", Left = 270, Width = 90, Top = 90, DialogResult = DialogResult.Cancel };

        // Set up "Enter" and "Esc" key behavior
        promptForm.AcceptButton = btnOk;
        promptForm.CancelButton = btnCancel;

        // Add controls to the form
        promptForm.Controls.Add(lblPrompt);
        promptForm.Controls.Add(txtInput);
        promptForm.Controls.Add(btnOk);
        promptForm.Controls.Add(btnCancel);

        // Show as a Modal Dialog
        if (promptForm.ShowDialog() == DialogResult.OK)
        {
            return txtInput.Text;
        }

        return ""; // Return empty string if cancelled, matching VB behavior
    }
}