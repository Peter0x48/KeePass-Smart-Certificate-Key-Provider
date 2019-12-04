namespace SmartCertificateKeyProviderPlugin
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using KeePass.Forms;
    using KeePass.Plugins;
    using KeePassLib.Keys;
    using KeePassLib.Utility;
    using SmartCertificateKeyProviderPlugin.Extensions;

    public class SmartCertificateKeyProvider : KeyProvider, IDisposable
    {
        #region Constants

        private const string DefaultSignatureDataText = "Data text for KeePass Password Safe Plugin - {F3EF424C-7517-4D58-A3FB-C1FB458FDDB6}!"; // DO NOT CHANGE THIS!!!!

        #endregion

        #region Private static properties

        private static X509Certificate2[] UserCertificates
        {
            get
            {
                var myStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                myStore.Open(OpenFlags.ReadOnly);

                var certificates = myStore.Certificates.Cast<X509Certificate2>()
                                          .Where(c => c.HasPrivateKey)
                                          .ToArray();

                myStore.Close();

                return certificates;
            }
        }

        #endregion

        #region Constructors

        public SmartCertificateKeyProvider(IPluginHost host)
        {
            DataToSign = Encoding.UTF8.GetBytes(DefaultSignatureDataText);
        }

        #endregion

        #region Public properties

        public override bool DirectKey => false; // DO NOT CHANGE THIS!!!!

        public override bool GetKeyMightShowGui => true;

        public override string Name => "Smart Certificate Key Provider";

        public override bool SecureDesktopCompatible => true;

        #endregion

        #region Private properties

        private byte[] DataToSign { get; }

        private IPluginHost Host { get; }

        #endregion

        #region Public methods

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public override byte[] GetKey(KeyProviderQueryContext keyProviderQueryContext)
        {
            // Read properties file
            string propertiesFilePath = keyProviderQueryContext.DatabasePath;
            if (!propertiesFilePath.EndsWith(".kdbx"))
            {
                MessageService.ShowWarning("Database file has wrong extension!");
                return null;
            }

            propertiesFilePath = propertiesFilePath.Replace(".kdbx", ".properties");

            SavedDatabaseProperties savedDatabaseProperties = new SavedDatabaseProperties(propertiesFilePath);

            // Read existing properties file
            if (!keyProviderQueryContext.CreatingNewKey)
            {
                try
                {
                    savedDatabaseProperties.ReadFile();
                }
                catch (Exception ex)
                {
                    MessageService.ShowWarning($"Unable to read database properties.\n{ex.Message}\nReason: You can now use the recovery form.");
                    byte[] readRecoveryKey = ReadRecoveryKey();
                    if (readRecoveryKey != null)
                    {
                        return readRecoveryKey;
                    }

                    MessageService.ShowWarning("Recovery failed.");
                    return null;
                }
            }
            else
            {
                // Create salt
                byte[] saltBytes = new byte[32];
                new Random().NextBytes(saltBytes);
                savedDatabaseProperties.PutValue("salt", StringExtension.ByteArrayToString(saltBytes));

                try
                {
                    savedDatabaseProperties.SaveFile();
                }
                catch (Exception ex)
                {
                    MessageService.ShowWarning($"Unable to save properties file.\nReason: {ex.Message}");
                    return null;
                }
            }

            string salt = savedDatabaseProperties.GetValue("salt");
            string usedCert = savedDatabaseProperties.GetValue("cert");

            X509Certificate2[] userCertificates = UserCertificates;
            if (userCertificates == null || userCertificates.Length == 0)
            {
                byte[] readRecoveryKey = ReadRecoveryKey();
                if (readRecoveryKey != null)
                {
                    return readRecoveryKey;
                }
            }

            X509Certificate2 certificate = null;

            if (usedCert != null)
            {
                certificate = userCertificates.SingleOrDefault(c => c.Thumbprint.GenerateSha256Hash().Equals(usedCert));
            }

            if (certificate == null)
            {
                var title = "Available certificates";
                var message = "Select certificate to use it for encryption on your KeePass database.";

                var x509Certificates = X509Certificate2UI.SelectFromCollection(new X509Certificate2Collection(userCertificates), title, message, X509SelectionFlag.SingleSelection)
                                                         .Cast<X509Certificate2>();

                certificate = x509Certificates.FirstOrDefault();
            }

            // Show recovery form if no certificate is returned
            if (certificate == null)
            {
                byte[] readRecoveryKey = ReadRecoveryKey();
                if (readRecoveryKey != null)
                {
                    return readRecoveryKey;
                }
            }

            if (certificate == null)
            {
                MessageService.ShowInfo("No valid certificate selected!");
                return null;
            }

            try
            {
                if (!(certificate.PrivateKey is RSA))
                {
                    MessageService.ShowWarning("PrivateKey of selected certificate is not RSA!");
                    return null;
                }

                RSA privateKey = (RSA)certificate.PrivateKey;

                byte[] dataToSign = Encoding.UTF8.GetBytes(string.Format("{0}:{1}", salt, DefaultSignatureDataText));

                byte[] signedData = privateKey.SignData(dataToSign, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1); // DO NOT CHANGE THIS!!!!;

                // Save certificate if needed
                if (usedCert == null || usedCert.Length == 0)
                {
                    savedDatabaseProperties.PutValue("cert", certificate.Thumbprint.GenerateSha256Hash());
                    try
                    {
                        savedDatabaseProperties.SaveFile();
                    }
                    catch (Exception ex)
                    {
                        MessageService.ShowWarning($"Unable to save properties file.\nReason: {ex.Message}");
                        return null;
                    }
                }

                // Show recovery form if this is initial signing
                if (keyProviderQueryContext.CreatingNewKey)
                {
                    SaveRecoveryKeyForm saveRecoveryKeyForm = new SaveRecoveryKeyForm(StringExtension.ByteArrayToString(signedData));
                    saveRecoveryKeyForm.ShowDialog();
                }

                return signedData;
            }
            catch (Exception ex)
            {
                MessageService.ShowWarning($"Selected certificate can't be used!\nReason: {ex.Message}.\nTrying recovery...");
                byte[] readRecoveryKey = ReadRecoveryKey();
                if (readRecoveryKey != null)
                {
                    return readRecoveryKey;
                }

                MessageService.ShowWarning("Recovery failed.");
                return null;
            }
        }

        #endregion

        #region Private methods

        private byte[] ReadRecoveryKey()
        {
            EnterRecoveryKeyForm enterRecoveryKeyForm = new EnterRecoveryKeyForm();
            if (enterRecoveryKeyForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                byte[] decodedData;
                try
                {
                    decodedData = StringExtension.StringToByteArray(enterRecoveryKeyForm.EnteredKey);
                }
                catch (Exception ex)
                {
                    MessageService.ShowWarning($"Unable to decode entered hex string.\nReason: {ex.Message}.");
                    return null;
                }

                return decodedData;
            }

            return null;
        }
        #endregion
    }
}