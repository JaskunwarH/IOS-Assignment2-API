using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using StockWiseAPI.Services;

namespace StockWiseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _client;
        private readonly LlmService _llmService;

        public StockController(IConfiguration config, LlmService llmService)
        {
            _config = config;
            _client = new HttpClient();
            _llmService = llmService;
        }

        // GET: api/stock/summary?symbol=AAPL
        [HttpGet("summary")]
        public async Task<IActionResult> GetStockSummary([FromQuery] string symbol = "AAPL")
        {
            string apiKey = _config["AlphaVantageKey"] ?? "";
            
            if (string.IsNullOrEmpty(apiKey) || apiKey == "<YOUR_ALPHA_VANTAGE_KEY>")
            {
                return StatusCode(500, "AlphaVantageKey is not configured. Please set the AlphaVantageKey in Azure App Service configuration.");
            }
            
            // string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={apiKey}";
            string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={apiKey}";

            try
            {
                Console.WriteLine("Requesting: " + url);

                var response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (root.TryGetProperty("Global Quote", out JsonElement quote))
                {
                    // Create the result object first
                    var result = new
                    {
                        Symbol = quote.GetProperty("01. symbol").GetString(),
                        Price = quote.GetProperty("05. price").GetString(),
                        Change = quote.GetProperty("09. change").GetString(),
                        PercentChange = quote.GetProperty("10. change percent").GetString()
                    };

                    // Then get AI summary from GitHub model
                    string summary = await _llmService.SummarizeStockAsync(
                        result.Symbol ?? "Unknown",
                        result.Price ?? "0",
                        result.Change ?? "0",
                        result.PercentChange ?? "0%"
                    );

                    var finalResult = new
                    {
                        result.Symbol,
                        result.Price,
                        result.Change,
                        result.PercentChange,
                        aiSummary = summary
                    };

                    return Ok(finalResult);
                }

                return NotFound("No stock data found for this symbol.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching stock data: {ex.Message}");
            }
        }
    }
}
