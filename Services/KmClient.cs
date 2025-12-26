using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http.Headers;

namespace QuMailClient
{
    public class KmClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        // --- CLOUD PRODUCTION CONFIGURATION ---
        // Switched from localhost to your live Render endpoint
        private const string KmUrl = "https://qumail-kms-server.onrender.com/api/keys/get_key";

        // This must match the API_KEY defined in your Python main.py
        private const string KmsApiKey = "QU-TERM-PRO-2025-SECURE-ACCESS";

        public async Task<(string KeyId, byte[] KeyBytes)> GetQuantumKeyAsync()
        {
            try
            {
                var requestData = new { size = 32 };
                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, KmUrl);
                request.Content = content;
                request.Headers.Add("X-API-KEY", KmsApiKey);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();

                    var data = JsonSerializer.Deserialize<KmsKeyResponse>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // FIXED CS8619: Handle nullability safely
                    if (data != null && !string.IsNullOrEmpty(data.KeyBuffer))
                    {
                        string id = data.KeyId ?? "KMS-ID-UNKNOWN";
                        byte[] key = Convert.FromBase64String(data.KeyBuffer);
                        return (id, key);
                    }
                }
                else
                {
                    // Updated message to reflect Cloud rejection
                    throw new Exception($"KMS_CLOUD_REJECTION: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Updated message to reflect Cloud connectivity issues
                throw new Exception($"KMS_CLOUD_UNREACHABLE: {ex.Message}");
            }

            return ("ERROR", Array.Empty<byte>());
        }
    }

    public class KmsKeyResponse
    {
        // FIXED CS8618: Mark properties as nullable
        public string? KeyId { get; set; }
        public string? KeyBuffer { get; set; }
    }
}