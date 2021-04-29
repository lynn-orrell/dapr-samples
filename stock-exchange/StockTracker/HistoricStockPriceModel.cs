using System;

namespace StockTracker
{
    public record HistoricStockPriceModel(string Symbol, decimal CurrentPrice, decimal HighPrice, decimal LowPrice);
}
