using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace QuMailClient
{
    public static class SecurityCore
    {
        // --- LEVEL 1: QUANTUM ONE-TIME PAD (OTP) ---
        // Professional Note: Truly secure OTP requires key.Length >= text.Length.
        // We use modular arithmetic to wrap the key if it's shorter (Stream Cipher mode).
        public static string EncryptWithOtp(string text, byte[] key)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            byte[] result = new byte[textBytes.Length];
            for (int i = 0; i < textBytes.Length; i++)
                result[i] = (byte)(textBytes[i] ^ key[i % key.Length]);
            return Convert.ToBase64String(result);
        }

        public static string DecryptWithOtp(string cipher, byte[] key)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipher);
            byte[] result = new byte[cipherBytes.Length];
            for (int i = 0; i < cipherBytes.Length; i++)
                result[i] = (byte)(cipherBytes[i] ^ key[i % key.Length]);
            return Encoding.UTF8.GetString(result);
        }

        // --- LEVEL 2: QUANTUM-AIDED AES-256 (CBC Mode) ---
        public static string EncryptWithAes(string text, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] input = Encoding.UTF8.GetBytes(text);
                    byte[] encrypted = encryptor.TransformFinalBlock(input, 0, input.Length);

                    // Prepend IV to the ciphertext so the receiver can extract it
                    byte[] combined = new byte[aes.IV.Length + encrypted.Length];
                    Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
                    Buffer.BlockCopy(encrypted, 0, combined, aes.IV.Length, encrypted.Length);
                    return Convert.ToBase64String(combined);
                }
            }
        }

        public static string DecryptWithAes(string ciphertext, byte[] key)
        {
            byte[] combined = Convert.FromBase64String(ciphertext);
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
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

        // --- LEVEL 3: POST-QUANTUM HYBRID (SIMULATED LATTICE-WRAPPED AES) ---
        // Implementation: Adds an HMAC-SHA256 signature to simulate a Lattice-based 
        // Message Authentication Code (MAC) verifying the AES payload.
        public static string EncryptPqcHybrid(string text, byte[] key)
        {
            // Stage 1: Standard Quantum-KMS AES
            string aesCipher = EncryptWithAes(text, key);

            // Stage 2: Simulate PQC Verification Tag (HMAC)
            using (var hmac = new HMACSHA256(key))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(aesCipher));
                string signature = Convert.ToBase64String(hash);
                // Return packet format: PQC_v1.[Signature].[Payload]
                return $"PQC_v1.{signature}.{aesCipher}";
            }
        }

        public static string DecryptPqcHybrid(string ciphertext, byte[] key)
        {
            if (!ciphertext.StartsWith("PQC_v1.")) throw new Exception("INVALID_PQC_PROTOCOL");

            string[] parts = ciphertext.Split('.');
            string signature = parts[1];
            string aesCipher = parts[2];

            // Verify Integrity (The "Lattice" check)
            using (var hmac = new HMACSHA256(key))
            {
                byte[] expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(aesCipher));
                string expectedSignature = Convert.ToBase64String(expectedHash);

                if (signature != expectedSignature)
                    throw new CryptographicException("PQC_INTEGRITY_VIOLATION: Packet may have been tampered with by a Quantum Interceptor.");
            }

            return DecryptWithAes(aesCipher, key);
        }

        // --- LEVEL 4: NO APPLICATION SECURITY (PLAINTEXT) ---
        public static string PlaintextMode(string text) => "PLAIN_" + Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        public static string DecodePlaintext(string ciphertext) => Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext.Replace("PLAIN_", "")));
    }
}