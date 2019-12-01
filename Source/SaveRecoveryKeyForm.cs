namespace SmartCertificateKeyProviderPlugin
{
    using System.Windows.Forms;

    public partial class SaveRecoveryKeyForm : Form
    {
        public SaveRecoveryKeyForm(string signedData)
        {
            InitializeComponent();
            this.recoveryKeyTextField.Text = signedData;
        }
    }
}
