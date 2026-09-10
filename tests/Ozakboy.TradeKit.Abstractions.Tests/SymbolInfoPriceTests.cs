namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 價格校正與交易規則檢查的測試。
/// Tests for price normalisation and trading-rule validation.
/// </summary>
[TestClass]
public sealed class SymbolInfoPriceTests
{
    [TestMethod]
    [DataRow("50000.14", "50000.1")]
    [DataRow("50000.16", "50000.2")]
    [DataRow("50000.10", "50000.1")]
    public void 預設取最接近的跳動點(string input, string expected)
    {
        var result = TestSymbols.Btc().NormalizePrice(TestSymbols.D(input));

        Assert.IsTrue(result.TryGetValue(out var price), result.Error?.ToString());
        Assert.AreEqual(TestSymbols.D(expected), price);
    }

    [TestMethod]
    public void 中點採用銀行家捨入()
    {
        var symbol = TestSymbols.Btc();

        Assert.IsTrue(symbol.NormalizePrice(50_000.25m).TryGetValue(out var down));
        Assert.IsTrue(symbol.NormalizePrice(50_000.15m).TryGetValue(out var up));

        Assert.AreEqual(50_000.2m, down);
        Assert.AreEqual(50_000.2m, up);
    }

    [TestMethod]
    public void 向下取整不會超過原價()
    {
        var result = TestSymbols.Btc().NormalizePrice(50_000.19m, PriceRounding.Down);

        Assert.IsTrue(result.TryGetValue(out var price), result.Error?.ToString());
        Assert.AreEqual(50_000.1m, price);
    }

    [TestMethod]
    public void 向上取整不會低於原價()
    {
        var result = TestSymbols.Btc().NormalizePrice(50_000.11m, PriceRounding.Up);

        Assert.IsTrue(result.TryGetValue(out var price), result.Error?.ToString());
        Assert.AreEqual(50_000.2m, price);
    }

    [TestMethod]
    public void 已對齊的價格不會被改動()
    {
        var symbol = TestSymbols.Btc();

        foreach (var rounding in new[] { PriceRounding.Nearest, PriceRounding.Down, PriceRounding.Up })
        {
            var result = symbol.NormalizePrice(50_000.3m, rounding);

            Assert.IsTrue(result.TryGetValue(out var price), result.Error?.ToString());
            Assert.AreEqual(50_000.3m, price, $"{rounding} 改動了已對齊的價格");
            Assert.IsTrue(symbol.IsPriceAligned(price));
        }
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("-1")]
    public void 非正數的價格直接回報不合法(string input)
    {
        var result = TestSymbols.Btc().NormalizePrice(TestSymbols.D(input));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidPrice, result.Error!.Code);
    }

    [TestMethod]
    public void 向下取整後歸零的價格回報不合法()
    {
        var symbol = TestSymbols.Btc() with { MinPrice = 0m };

        var result = symbol.NormalizePrice(0.05m, PriceRounding.Down);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidPrice, result.Error!.Code);
    }

    [TestMethod]
    public void 低於最低價時回報超出範圍()
    {
        var result = TestSymbols.Btc().NormalizePrice(99.9m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.PriceOutOfRange, result.Error!.Code);
    }

    [TestMethod]
    public void 高於最高價時回報超出範圍()
    {
        var result = TestSymbols.Btc().NormalizePrice(1_000_000.06m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.PriceOutOfRange, result.Error!.Code);
    }

    [TestMethod]
    public void 恰好等於上下限時通過()
    {
        var symbol = TestSymbols.Btc();

        Assert.IsTrue(symbol.NormalizePrice(100m).IsSuccess);
        Assert.IsTrue(symbol.NormalizePrice(1_000_000m).IsSuccess);
    }

    [TestMethod]
    public void 未定義的取整方向視為程式缺陷擲出例外()
    {
        var symbol = TestSymbols.Btc();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => symbol.NormalizePrice(50_000m, (PriceRounding)99));
    }

    [TestMethod]
    public void 一次校正價量並回報名目價值()
    {
        var result = TestSymbols.Btc().NormalizeOrderSize(50_000.16m, 0.0025m);

        Assert.IsTrue(result.TryGetValue(out var size), result.Error?.ToString());
        Assert.AreEqual(50_000.2m, size.Price);
        Assert.AreEqual(0.002m, size.Quantity);
        Assert.AreEqual(size.Price * size.Quantity, size.Notional);
    }

    [TestMethod]
    public void 價格不合格時整筆校正失敗()
    {
        var result = TestSymbols.Btc().NormalizeOrderSize(99.9m, 1m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.PriceOutOfRange, result.Error!.Code);
    }

    [TestMethod]
    public void 名目價值不足時整筆校正失敗()
    {
        var result = TestSymbols.Btc().NormalizeOrderSize(50_000m, 0.001m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.NotionalBelowMinimum, result.Error!.Code);
    }

    [TestMethod]
    public void 步進值不合法時所有校正方法都回報規則錯誤()
    {
        var broken = TestSymbols.Btc() with { TickSize = 0m, StepSize = 0m };

        Assert.AreEqual(TradeErrorCodes.InvalidSymbolRules, broken.NormalizePrice(1m).Error!.Code);
        Assert.AreEqual(TradeErrorCodes.InvalidSymbolRules, broken.NormalizeQuantity(1m).Error!.Code);
        Assert.AreEqual(TradeErrorCodes.InvalidSymbolRules, broken.NormalizeOrderSize(1m, 1m).Error!.Code);
        Assert.AreEqual(TradeErrorCodes.InvalidSymbolRules, broken.GetMinimumQuantity(1m).Error!.Code);
        Assert.IsFalse(broken.IsPriceAligned(1m));
        Assert.IsFalse(broken.IsQuantityAligned(1m));
        Assert.AreEqual(0, broken.PriceScale);
        Assert.AreEqual(0, broken.QuantityScale);
    }

    [TestMethod]
    public void 小數位數由步進值推得()
    {
        var symbol = TestSymbols.Btc() with { TickSize = 0.0100m, StepSize = 0.00100000m };

        Assert.AreEqual(2, symbol.PriceScale);
        Assert.AreEqual(3, symbol.QuantityScale);
    }

    [TestMethod]
    public void 序列化不得出現科學記號()
    {
        Assert.AreEqual("0.00001", SymbolInfo.FormatPrice(0.00001m));
        Assert.AreEqual("0.001", SymbolInfo.FormatQuantity(0.00100m));
    }

    [TestMethod]
    public void 合法的交易規則通過檢查()
    {
        Assert.IsTrue(TestSymbols.Btc().Validate().IsSuccess);
    }

    [TestMethod]
    public void 不合法的交易規則被檢查擋下()
    {
        var symbol = TestSymbols.Btc();

        SymbolInfo[] broken =
        [
            symbol with { Name = "  " },
            symbol with { BaseAsset = "" },
            symbol with { QuoteAsset = "" },
            symbol with { TickSize = 0m },
            symbol with { StepSize = -1m },
            symbol with { MinQuantity = 10m, MaxQuantity = 1m },
            symbol with { MaxQuantity = 0m },
            symbol with { MinPrice = 10m, MaxPrice = 1m },
            symbol with { MaxPrice = 0m },
            symbol with { MinNotional = -1m },
            symbol with { MaxLeverage = 0 },
        ];

        foreach (var candidate in broken)
        {
            var result = candidate.Validate();

            Assert.IsTrue(result.IsFailure, $"{candidate} 應該被擋下");
            Assert.AreEqual(TradeErrorCodes.InvalidSymbolRules, result.Error!.Code);
        }
    }
}
