
namespace XMMTwrapper1
{
    public partial class frmSetCred : Form
    {
        private CredentialVault _vault;
        public frmSetCred(CredentialVault vault)
        {
            _vault = vault;
            InitializeComponent();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Please enter both username and password.", "Input Error");
                return;
            }

            // Save to Windows Credential Manager
            _vault.Store(txtUsername.Text, txtPassword.Text);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void FrmSettings_Load(object sender, EventArgs e)
        {
            // Pre-fill if credentials already exist
            var creds = _vault.Retrieve();
            if (creds != null)
            {
                txtUsername.Text = creds.Value.user;
                txtPassword.Text = creds.Value.pass;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
