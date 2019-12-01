namespace SmartCertificateKeyProviderPlugin
{
    using System;
    using System.Collections.Generic;

    internal sealed class SavedDatabaseProperties
    {
        private static readonly string[] REQUIREDFIELDS = { "salt" };

        private readonly string filePath;
        private readonly Dictionary<string, string> pairs = new Dictionary<string, string>();

        public SavedDatabaseProperties(string filePath)
        {
            this.filePath = filePath;
        }

        public bool FileExists()
        {
            return System.IO.File.Exists(this.filePath);
        }

        public void ReadFile()
        {
            if (!FileExists())
            {
                throw new Exception("File does not exists.");
            }

            string[] lines = System.IO.File.ReadAllLines(filePath);
            if (lines == null || lines.Length == 0)
            {
                throw new Exception("Property file is not valid.");
            }

            foreach (string line in lines)
            {
                string[] splitted = line.Split('=');
                if (splitted.Length != 2)
                {
                    continue;
                }

                string key = splitted[0];
                string value = splitted[1];

                this.pairs.Add(key, value);
            }

            // Check if required fields are set
            foreach (string key in REQUIREDFIELDS)
            {
                if (!this.pairs.ContainsKey(key))
                {
                    throw new Exception(string.Format("Missing key {0}", key));
                }
            }
        }

        public void SaveFile()
        {
            string[] content = new string[this.pairs.Count];
            int cur = 0;
            foreach (var entry in this.pairs)
            {
                content[cur++] = string.Format("{0}={1}", entry.Key, entry.Value);
            }

            System.IO.File.WriteAllLines(this.filePath, content);
        }

        public string GetValue(string key)
        {
            if (!this.pairs.ContainsKey(key))
            {
                return null;
            }

            return this.pairs[key];
        }

        public void PutValue(string key, string value)
        {
            this.pairs.Add(key, value);
        }
    }
}
