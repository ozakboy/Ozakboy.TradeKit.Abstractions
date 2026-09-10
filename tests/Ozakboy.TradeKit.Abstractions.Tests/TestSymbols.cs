using System.Globalization;

namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 測試共用的交易對規則。
/// Shared symbol rules for the tests.
/// </summary>
internal static class TestSymbols
{
    /// <summary>
    /// 一組貼近幣安 BTCUSDT 永續合約的規則:價格步進 0.1、數量步進 0.001、最小名目價值 100。
    /// Rules close to the Binance BTCUSDT perpetual: tick 0.1, step 0.001, minimum notional 100.
    /// </summary>
    /// <returns>交易規則。The symbol rules.</returns>
    public static SymbolInfo Btc() => new()
    {
        Name = "BTCUSDT",
        BaseAsset = "BTC",
        QuoteAsset = "USDT",
        TickSize = 0.1m,
        StepSize = 0.001m,
        MinQuantity = 0.001m,
        MaxQuantity = 1000m,
        MinPrice = 100m,
        MaxPrice = 1_000_000m,
        MinNotional = 100m,
        MaxLeverage = 125,
    };

    /// <summary>
    /// 把測試資料裡的字串轉成 <see cref="decimal"/>,固定使用不變文化。
    /// Parses a decimal from test data using the invariant culture.
    /// </summary>
    /// <param name="value">數值字串。The numeric string.</param>
    /// <returns>對應的數值。The parsed value.</returns>
    public static decimal D(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
