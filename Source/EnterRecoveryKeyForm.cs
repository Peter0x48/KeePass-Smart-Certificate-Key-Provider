namespace SmartCertificateKeyProviderPlugin
{
    using System.Windows.Forms;

    public partial class EnterRecoveryKeyForm : Form
    {
        public string EnteredKey
        {
            get;
            private set;
        }

        public EnterRecoveryKeyForm()
        {
            InitializeComponent();
        }

        private void EnterRecoveryKeyForm_Close(object o, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (this.recoveryKeyTextField.Text.Length == 0)
                {
                    MessageBox.Show("Error: Recovery Key text field is empty!");
                    e.Cancel = true;
                    return;
                }

                this.EnteredKey = this.recoveryKeyTextField.Text;
            }
        }
    }
}
