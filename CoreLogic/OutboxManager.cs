using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace QuMailClient
{
    public class OutboxEntry
    {
        public DateTime Timestamp { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Ciphertext { get; set; } = string.Empty;
        public string KeyId { get; set; } = string.Empty;
    }

    public static class OutboxManager
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuMailClient", "outbox.dat");

        /// <summary>
        /// Deletes the local outbox file. 
        /// Call this on app startup to ensure each session starts empty.
        /// </summary>
        public static void ClearHistory()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing outbox: {ex.Message}");
            }
        }

        public static List<OutboxEntry> LoadEntries()
        {
            // If file doesn't exist (because we cleared it), return an empty list
            if (!File.Exists(FilePath)) return new List<OutboxEntry>();

            try
            {
                byte[] encryptedData = File.ReadAllBytes(FilePath);
                byte[] decryptedData = ProtectedData.Unprotect(
                    encryptedData, null, DataProtectionScope.CurrentUser);

                string json = Encoding.UTF8.GetString(decryptedData);
                return JsonSerializer.Deserialize<List<OutboxEntry>>(json) ?? new List<OutboxEntry>();
            }
            catch
            {
                return new List<OutboxEntry>();
            }
        }

        public static void LogMessage(string recipient, string subject, string ciphertext, string keyId)
        {
            var entries = LoadEntries();

            entries.Add(new OutboxEntry
            {
                Timestamp = DateTime.Now,
                Recipient = recipient,
                Subject = subject,
                Ciphertext = ciphertext,
                KeyId = keyId
            });

            string json = JsonSerializer.Serialize(entries);
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(json);

            byte[] encryptedData = ProtectedData.Protect(
                dataToEncrypt, null, DataProtectionScope.CurrentUser);

            // Ensure directory exists (important if this is the first log of a fresh session)
            string? directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(FilePath, encryptedData);
        }
    }
}
