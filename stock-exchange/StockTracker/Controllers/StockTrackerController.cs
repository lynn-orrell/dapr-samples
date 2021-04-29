using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace StockTracker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StockTrackerController : ControllerBase
    {
        private readonly ILogger<StockTrackerController> _logger;
        private readonly HttpClient _httpClient;
        public StockTrackerController(IHttpClientFactory httpClientFactory, ILogger<StockTrackerController> logger)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost()]
        public async Task<IActionResult> ReceiveStockPrice(CloudEvent cloudEvent)
        {
            var stockPrice = ((JToken)cloudEvent.Data).ToObject<StockPriceModel>();
            _logger.LogInformation($"Recieved Current Price: {stockPrice}");

            HistoricStockPriceModel historicStockPrice = await GetHistoricStockPriceAsync(stockPrice.Symbol);
            if(historicStockPrice == null)
            {
                historicStockPrice = new HistoricStockPriceModel(stockPrice.Symbol, stockPrice.Price, stockPrice.Price, stockPrice.Price);
            }
            else
            {
                historicStockPrice = historicStockPrice with { 
                                                                 CurrentPrice = stockPrice.Price, 
                                                                 LowPrice = Math.Min(historicStockPrice.LowPrice, stockPrice.Price),
                                                                 HighPrice = Math.Max(historicStockPrice.HighPrice, stockPrice.Price)
                                                             };
            }

            await SaveHistoricStockPrice(historicStockPrice);

            return new OkResult();
        }

        [HttpGet("{symbol}")]
        public async Task<IActionResult> Get(string symbol)
        {
            var historicStockPrice = await GetHistoricStockPriceAsync(symbol);
            return historicStockPrice == null ? NoContent() : Ok(historicStockPrice);
        }

        private async Task<HistoricStockPriceModel> GetHistoricStockPriceAsync(string symbol)
        {
            var response = await _httpClient.GetAsync($"http://localhost:5080/v1.0/state/stocktracker-state/{symbol}");
            
            switch(response.StatusCode)
            {
                case HttpStatusCode.OK:
                {
                    var cachedHistoricStockPrice = JsonSerializer.Deserialize<HistoricStockPriceModel>(await response.Content.ReadAsByteArrayAsync());
                    _logger.LogInformation($"Read Cached Stock: {cachedHistoricStockPrice}");
                    return cachedHistoricStockPrice;
                }
                case HttpStatusCode.NoContent:
                {
                    return null;
                }
                default:
                {
                    throw new Exception($"Status Code {response.StatusCode} received. Message: {await response.Content.ReadAsStringAsync()}");
                }
            }
        }

        private async Task SaveHistoricStockPrice(HistoricStockPriceModel historicStockPrice)
        {
            var response = await _httpClient.PostAsync("http://localhost:5080/v1.0/state/stocktracker-state", 
                                                       new StringContent(JsonSerializer.Serialize(new[]
                                                       {
                                                           new { key = historicStockPrice.Symbol, value = historicStockPrice }
                                                       }), 
                                                       Encoding.UTF8, "application/json"));

            if(!response.IsSuccessStatusCode)
            {
                throw new Exception($"Status Code {response.StatusCode} received. Message: {await response.Content.ReadAsStringAsync()}");
            }

            _logger.LogInformation($"Updated Cached Stock: {historicStockPrice}");
        }
    }
}
