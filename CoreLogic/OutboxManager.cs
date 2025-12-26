using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography; // Required for DPAPI
using System.Text;

namespace QuMailClient
{
    public class OutboxEntry
    {
        public DateTime Timestamp { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Ciphertext { get; set; } = string.Empty; // Changed from 'Data' to 'Ciphertext'
        public string KeyId { get; set; } = string.Empty;
    }

    public static class OutboxManager
    {
        // Path to store the encrypted history file in the user's AppData folder
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuMailClient", "outbox.dat");

        /// <summary>
        /// Loads and decrypts the sent message history from the local disk.
        /// </summary>
        public static List<OutboxEntry> LoadEntries()
        {
            if (!File.Exists(FilePath)) return new List<OutboxEntry>();

            try
            {
                byte[] encryptedData = File.ReadAllBytes(FilePath);

                // Decrypt the data using the current Windows user's credentials
                byte[] decryptedData = ProtectedData.Unprotect(
                    encryptedData, null, DataProtectionScope.CurrentUser);

                string json = Encoding.UTF8.GetString(decryptedData);
                return JsonSerializer.Deserialize<List<OutboxEntry>>(json) ?? new List<OutboxEntry>();
            }
            catch
            {
                // If decryption fails (e.g., file corrupted or different user), return empty list
                return new List<OutboxEntry>();
            }
        }

        /// <summary>
        /// Encrypts and saves a new message entry to the persistent local outbox.
        /// </summary>
        public static void LogMessage(string recipient, string subject, string ciphertext, string keyId)
        {
            // 1. Load existing entries first
            var entries = LoadEntries();

            // 2. Add the new entry
            entries.Add(new OutboxEntry
            {
                Timestamp = DateTime.Now,
                Recipient = recipient,
                Subject = subject,
                Ciphertext = ciphertext,
                KeyId = keyId
            });

            // 3. Serialize and Encrypt the entire list
            string json = JsonSerializer.Serialize(entries);
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(json);

            byte[] encryptedData = ProtectedData.Protect(
                dataToEncrypt, null, DataProtectionScope.CurrentUser);

            // 4. Ensure directory exists and write to file
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllBytes(FilePath, encryptedData);
        }
    }
}