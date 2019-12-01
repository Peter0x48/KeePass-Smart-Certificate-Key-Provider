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
            Host = host;
            CertificateCache = new UsedCertificateCache();

            Host.MainWindow.FileOpened += OnDatabaseOpened;
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

        private UsedCertificateCache CertificateCache { get; }

        private byte[] DataToSign { get; }

        private IPluginHost Host { get; }

        #endregion

        #region Public methods

        public void Dispose()
        {
            Host.MainWindow.FileOpened -= OnDatabaseOpened;

            CertificateCache.Dispose();

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
                savedDatabaseProperties.PutValue("salt", ByteArrayToString(saltBytes));

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

            if (!keyProviderQueryContext.CreatingNewKey)
                certificate = GetCertificateFromCache(keyProviderQueryContext.DatabasePath);

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
                MessageService.ShowInfo("No valid certificate selected!");
            else
            {
                try
                {
                    if (certificate.PrivateKey is RSA rsa)
                    {
                        CertificateCache.StoreCachedValue(keyProviderQueryContext.DatabasePath, certificate.Thumbprint);

                        byte[] dataToSign = Encoding.UTF8.GetBytes(string.Format("{0}:{1}", salt, DefaultSignatureDataText));

                        byte[] signedData = rsa.SignData(dataToSign, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1); // DO NOT CHANGE THIS!!!!;

                        // Show recovery form if this is initial signing
                        if (keyProviderQueryContext.CreatingNewKey)
                        {
                            SaveRecoveryKeyForm saveRecoveryKeyForm = new SaveRecoveryKeyForm(ByteArrayToString(signedData));
                            saveRecoveryKeyForm.ShowDialog();
                        }

                        return signedData;
                    }
                }
                catch (Exception ex)
                {
                    MessageService.ShowWarning($"Selected certificate can't be used!\nReason: {ex.Message}.");
                }
            }

            return null;
        }

        #endregion

        #region Private methods

        private X509Certificate2 GetCertificateFromCache(string databasePath)
        {
            try
            {
                var thumbprint = CertificateCache.GetCachedValue(databasePath);

                if (thumbprint != null)
                    return UserCertificates.SingleOrDefault(c => c.Thumbprint.Equals(thumbprint));
            }
            catch (Exception ex)
            {
                MessageService.ShowWarning($"Selected certificate can't be used!\nReason: {ex.Message}.");
            }

            return null;
        }

        private byte[] ReadRecoveryKey()
        {
            EnterRecoveryKeyForm enterRecoveryKeyForm = new EnterRecoveryKeyForm();
            if (enterRecoveryKeyForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                byte[] decodedData;
                try
                {
                    decodedData = StringToByteArray(enterRecoveryKeyForm.EnteredKey);
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

        // Taken from: https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
        private string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private byte[] StringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private void OnDatabaseOpened(object sender, FileOpenedEventArgs args)
        {
            var path = args.Database.IOConnectionInfo.Path;

            CertificateCache.SetCachedItemAsValid(path);
        }

        #endregion
    }
}