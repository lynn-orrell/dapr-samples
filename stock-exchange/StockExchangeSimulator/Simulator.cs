using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StockExchangeSimulator
{
    public class Simulator
    {
        static HttpClient _httpClient = new HttpClient();
        readonly Random _random = new Random();
        readonly StockModel[] _stockModels;

        public Simulator(StockModel[] stockModels)
        {
            _stockModels = stockModels;
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            await Task.Run(async () => 
            {
                StockModel stock;
                while(!cancellationToken.IsCancellationRequested)
                {
                    for(int x = 0; x < _stockModels.Length; x++)
                    {
                        stock = _stockModels[x];
                        decimal price = new decimal(_random.Next(stock.MinPrice, stock.MaxPrice - 1) + _random.NextDouble());
                        
                        Console.WriteLine($"{stock.Symbol}: {price}");
                        
                        var response = await _httpClient.PostAsync("http://localhost:5180/v1.0/publish/stocksim-pubsub/stockprices", 
                                                                    new StringContent(JsonSerializer.Serialize(new { Symbol = stock.Symbol, Price = price }), 
                                                                                      Encoding.UTF8, "application/json"));
                        
                        if(!response.IsSuccessStatusCode)
                        {
                            Console.Error.WriteLine($"Status Code {response.StatusCode} received. Message: {await response.Content.ReadAsStringAsync()}");
                        }


                        await Task.Delay(1000, cancellationToken);
                    }
                }
            }, cancellationToken);
        }
    }
}
