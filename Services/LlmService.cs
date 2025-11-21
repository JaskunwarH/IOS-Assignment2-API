using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StockWiseAPI.Services
{
    public class LlmService
    {
        private readonly HttpClient _client;
        private readonly string _token;
        private readonly string _endpoint;
        private readonly string _model;

        public LlmService(IConfiguration config)
        {
            _client = new HttpClient();
            _token = config["GitHubToken"] ?? throw new Exception("GitHubToken not found in configuration.");
            string baseEndpoint = config["LlmEndpoint"] ?? "https://models.github.ai/inference";
            // Append /v1/chat/completions if not already present
            _endpoint = baseEndpoint.EndsWith("/v1/chat/completions") 
                ? baseEndpoint 
                : baseEndpoint.TrimEnd('/') + "/v1/chat/completions";
            // GitHub Models API requires model name with provider prefix (e.g., "openai/gpt-4o-mini")
            _model = config["LlmModel"] ?? "openai/gpt-5";

            // Required headers for GitHub Models API (models.github.ai uses simpler headers)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            _client.DefaultRequestHeaders.Add("User-Agent", "StockWiseAPI/1.0");
            // Use standard JSON accept header for models.github.ai
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> SummarizeStockAsync(string symbol, string price, string change, string percentChange)
        {
            string prompt = $"Write a short summary about this stock performance: " +
                            $"{symbol} is trading at {price}, showing a change of {change} ({percentChange}). " +
                            $"Explain briefly whether the trend seems positive or negative.";

            var body = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "You are a financial assistant that summarizes stock trends clearly." },
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // If no token is configured, return a basic summary
                if (string.IsNullOrEmpty(_token))
                {
                    Console.WriteLine("GitHub token not configured, using fallback summary");
                    return $"Stock {symbol} is currently priced at ${price}, with a change of {change} ({percentChange}).";
                }

                Console.WriteLine($"Calling LLM endpoint: {_endpoint}");
                Console.WriteLine($"Using model: {_model}");
                Console.WriteLine($"Request body: {json}");
                Console.WriteLine($"Authorization header: Bearer {_token.Substring(0, Math.Min(20, _token.Length))}...");
                var response = await _client.PostAsync(_endpoint, content);

                Console.WriteLine("LLM status: " + response.StatusCode);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("LLM response: " + responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"LLM API error: {response.StatusCode} - {responseContent}");
                    return $"Stock {symbol} is currently priced at ${price}, with a change of {change} ({percentChange}).";
                }

                using var doc = JsonDocument.Parse(responseContent);
                if (doc.RootElement.TryGetProperty("choices", out JsonElement choices) &&
                    choices.GetArrayLength() > 0)
                {
                    string summary = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "No summary generated.";
                    return summary;
                }

                return $"Stock {symbol} is currently priced at ${price}, with a change of {change} ({percentChange}).";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LLM service error: {ex.Message}");
                return $"Stock {symbol} is currently priced at ${price}, with a change of {change} ({percentChange}).";
            }
        }
    }
}