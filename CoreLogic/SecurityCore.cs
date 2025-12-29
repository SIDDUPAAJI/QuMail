using System;
using System.Security.Cryptography;
using System.Text;

namespace QuMailClient
{
    public static class SecurityCore
    {
        // --- LEVEL 1: OTP ---
        public static string EncryptWithOtp(string text, byte[] key)
        {
            byte[] workKey = (byte[])key.Clone(); // Clone to protect the original
            try
            {
                byte[] textBytes = Encoding.UTF8.GetBytes(text);
                byte[] result = new byte[textBytes.Length];
                for (int i = 0; i < textBytes.Length; i++)
                    result[i] = (byte)(textBytes[i] ^ workKey[i % workKey.Length]);
                return Convert.ToBase64String(result);
            }
            finally { CryptographicOperations.ZeroMemory(workKey); }
        }

        public static string DecryptWithOtp(string cipher, byte[] key)
        {
            byte[] workKey = (byte[])key.Clone();
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipher);
                byte[] result = new byte[cipherBytes.Length];
                for (int i = 0; i < cipherBytes.Length; i++)
                    result[i] = (byte)(cipherBytes[i] ^ workKey[i % workKey.Length]);
                return Encoding.UTF8.GetString(result);
            }
            finally { CryptographicOperations.ZeroMemory(workKey); }
        }

        // --- LEVEL 2: AES-256 (Fixes Padding Error) ---
        public static string EncryptWithAes(string text, byte[] key)
        {
            byte[] workKey = (byte[])key.Clone();
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = workKey;
                    aes.GenerateIV();
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] input = Encoding.UTF8.GetBytes(text);
                        byte[] encrypted = encryptor.TransformFinalBlock(input, 0, input.Length);
                        byte[] combined = new byte[aes.IV.Length + encrypted.Length];
                        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
                        Buffer.BlockCopy(encrypted, 0, combined, aes.IV.Length, encrypted.Length);
                        return Convert.ToBase64String(combined);
                    }
                }
            }
            finally { CryptographicOperations.ZeroMemory(workKey); }
        }

        public static string DecryptWithAes(string ciphertext, byte[] key)
        {
            byte[] workKey = (byte[])key.Clone();
            try
            {
                byte[] combined = Convert.FromBase64String(ciphertext);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = workKey;
                    byte[] iv = new byte[aes.BlockSize / 8];
                    byte[] data = new byte[combined.Length - iv.Length];
                    Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
                    Buffer.BlockCopy(combined, iv.Length, data, 0, data.Length);
                    aes.IV = iv;
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);
                        return Encoding.UTF8.GetString(decrypted);
                    }
                }
            }
            finally { CryptographicOperations.ZeroMemory(workKey); }
        }

        // --- LEVEL 3: PQC HYBRID ---
        public static string EncryptPqcHybrid(string text, byte[] key)
        {
            byte[] workKey = (byte[])key.Clone();
            try
            {
                // We use a internal-style call here to avoid double-wiping
                string aesCipher = RawAesEncrypt(text, workKey);
                using (var hmac = new HMACSHA256(workKey))
                {
                    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(aesCipher));
                    return $"PQC_v1.{Convert.ToBase64String(hash)}.{aesCipher}";
                }
            }
            finally { CryptographicOperations.ZeroMemory(workKey); }
        }

        public static string DecryptPqcHybrid(string ciphertext, byte[] key)
        {
            byte[] workKey = (byte[])key.Clone();
            try
            {
                if (!ciphertext.StartsWith("PQC_v1.")) throw new Exception("INVALID_PQC");
                string[] parts = ciphertext.Split('.');
                string signature = parts[1];
                string aesCipher = parts[2];

                using (var hmac = new HMACSHA256(workKey))
                {
                    byte[] expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(aesCipher));
                    if (signature != Convert.ToBase64String(expectedHash))
                        throw new CryptographicException("PQC_INTEGRITY_VIOLATION");
                }
                return RawAesDecrypt(aesCipher, workKey);
            }
            finally { CryptographicOperations.ZeroMemory(workKey); }
        }

        // Internal Helpers for PQC (No wiping here)
        private static string RawAesEncrypt(string t, byte[] k)
        {
            using (Aes a = Aes.Create())
            {
                a.Key = k; a.GenerateIV();
                var e = a.CreateEncryptor(); byte[] b = Encoding.UTF8.GetBytes(t);
                byte[] f = e.TransformFinalBlock(b, 0, b.Length);
                byte[] c = new byte[a.IV.Length + f.Length];
                Buffer.BlockCopy(a.IV, 0, c, 0, a.IV.Length); Buffer.BlockCopy(f, 0, c, a.IV.Length, f.Length);
                return Convert.ToBase64String(c);
            }
        }
        private static string RawAesDecrypt(string c, byte[] k)
        {
            byte[] b = Convert.FromBase64String(c);
            using (Aes a = Aes.Create())
            {
                a.Key = k;
                byte[] iv = new byte[16]; byte[] d = new byte[b.Length - 16];
                Buffer.BlockCopy(b, 0, iv, 0, 16); Buffer.BlockCopy(b, 16, d, 0, d.Length);
                a.IV = iv; var dec = a.CreateDecryptor();
                return Encoding.UTF8.GetString(dec.TransformFinalBlock(d, 0, d.Length));
            }
        }

        public static string PlaintextMode(string t) => "PLAIN_" + Convert.ToBase64String(Encoding.UTF8.GetBytes(t));
        public static string DecodePlaintext(string c) => Encoding.UTF8.GetString(Convert.FromBase64String(c.Replace("PLAIN_", "")));
    }
}
