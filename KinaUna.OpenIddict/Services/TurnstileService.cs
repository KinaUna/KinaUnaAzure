using KinaUna.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KinaUna.OpenIddict.Services
{
    public class TurnstileService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TurnstileService> logger) : ITurnstileService
    {
        private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        public async Task<bool> VerifyTokenAsync(string token, string? remoteIp = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogWarning("Turnstile token is empty or null.");
                return false;
            }

            string secretKey = configuration.GetValue<string>(AuthConstants.TurnstileSecretKeyConfigKey) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                logger.LogError("TurnstileSecretKey is not configured. Skipping Turnstile verification.");
                // Fail open only if not configured — remove this return to fail closed instead.
                return true;
            }

            try
            {
                using HttpClient httpClient = httpClientFactory.CreateClient();

                Dictionary<string, string> formData = new()
                {
                    { "secret", secretKey },
                    { "response", token }
                };

                if (!string.IsNullOrWhiteSpace(remoteIp))
                {
                    formData.Add("remoteip", remoteIp);
                }

                using FormUrlEncodedContent content = new(formData);
                HttpResponseMessage response = await httpClient.PostAsync(VerifyUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Turnstile verification request failed with status code {StatusCode}.", response.StatusCode);
                    return false;
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                TurnstileVerifyResponse? result = JsonSerializer.Deserialize<TurnstileVerifyResponse>(responseBody);

                if (result == null)
                {
                    logger.LogWarning("Failed to deserialize Turnstile verification response.");
                    return false;
                }

                if (!result.Success)
                {
                    logger.LogWarning("Turnstile verification failed. Error codes: {ErrorCodes}", string.Join(", ", result.ErrorCodes ?? []));
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception during Turnstile verification.");
                return false;
            }
        }
    }

    internal class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTs { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }
    }
}
