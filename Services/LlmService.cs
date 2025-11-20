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
            _token = config["GitHubToken"] ?? "";
            _endpoint = config["LlmEndpoint"] ?? "https://models.inference.ai.azure.com/v1/chat/completions";
            _model = config["LlmModel"] ?? "gpt-4o-mini";

            if (!string.IsNullOrEmpty(_token))
            {
                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _token);
            }
        }

        public async Task<string> SummarizeStockAsync(string symbol, string price, string change, string percentChange)
        {
            // Prompt sent to the model
            string prompt = $"Summarize this stock update in one sentence: " +
                            $"Stock {symbol} is priced at {price}, changed by {change} ({percentChange}).";

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
                var response = await _client.PostAsync(_endpoint, content);

                Console.WriteLine("LLM status: " + response.StatusCode);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("LLM response: " + responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"LLM API error: {response.StatusCode} - {responseContent}");
                    // Return a fallback summary if LLM fails
                    return $"Stock {symbol} is currently priced at ${price}, with a change of {change} ({percentChange}).";
                }

                using var doc = JsonDocument.Parse(responseContent);
                if (doc.RootElement.TryGetProperty("choices", out JsonElement choices) && 
                    choices.GetArrayLength() > 0)
                {
                    string summary = choices[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? "No summary generated.";
                    return summary;
                }

                return $"Stock {symbol} is currently priced at ${price}, with a change of {change} ({percentChange}).";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LLM service error: {ex.Message}");
                // Return a fallback summary if LLM fails
                return $"Stock {symbol} is currently priced at ${price}, with a change of {change} ({percentChange}).";
            }
        }
    }
}
