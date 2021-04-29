using System;
using System.Threading;
using System.Threading.Tasks;

namespace StockExchangeSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            
            StockModel[] stocks = new StockModel[]
            {
                new StockModel("MSFT", 170, 262),
                new StockModel("AAPL", 68, 145),
                new StockModel("NVDA", 275, 648),
                new StockModel("TSLA", 136, 900),
                new StockModel("FB", 178, 316)
            };

            Simulator sim = new Simulator(stocks);
            Task simTask = sim.Start(token);

            Console.ReadKey();

            tokenSource.Cancel();
        }
    }
}
