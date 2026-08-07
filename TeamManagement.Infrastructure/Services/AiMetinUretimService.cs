using System.Net.Http;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using TeamManagement.Application.Interfaces;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    public class AiMetinUretimService : IAiMetinUretimService
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;

        //429 (rate limit) hatasında denenecek bekleme süreleri
        private static readonly int[] RetryBeklemeSaniye = { 5, 15 };

        public AiMetinUretimService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _model = configuration["Gemini:Model"] ?? "gemini-3.5-flash";
        }

        //429 alındığında kısa bir bekleme sonrası en fazla RetryBeklemeSaniye.Length kez tekrar dener
        private async Task<HttpResponseMessage> PostWithRetryAsync(string url, HttpContent content)
        {
            for (var deneme = 0; ; deneme++)
            {
                var response = await _httpClient.PostAsync(url, content);
                if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests || deneme >= RetryBeklemeSaniye.Length)
                    return response;

                await Task.Delay(TimeSpan.FromSeconds(RetryBeklemeSaniye[deneme]));
            }
        }

        public async Task<Result<string>> MetinUretAsync(string prompt, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return Result<string>.Failure("Gemini API Key bulunamadı.");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await PostWithRetryAsync(url, jsonContent);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Result<string>.Failure($"Gemini API hatası ({(int)response.StatusCode}): {responseString}");

            using var document = JsonDocument.Parse(responseString);
            var textResult = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            if (string.IsNullOrEmpty(textResult))
                return Result<string>.Failure("Modelden geçerli bir yanıt alınamadı.");

            return Result<string>.Success(textResult);
        }
    }
}
