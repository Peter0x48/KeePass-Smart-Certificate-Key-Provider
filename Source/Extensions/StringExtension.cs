namespace SmartCertificateKeyProviderPlugin.Extensions
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public static class StringExtension
    {
        #region Static Public methods

        public static string GenerateSha256Hash(this string text)
        {
            using (var sha = new SHA256Cng())
            {
                byte[] computeHash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));

                return ByteArrayToString(computeHash);
            }
        }

        // Taken from: https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] StringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        #endregion
    }
}