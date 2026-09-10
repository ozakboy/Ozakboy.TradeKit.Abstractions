namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 數量校正的測試。這是本套件唯一有實質邏輯的地方,也是風控的最後一道本地防線。
/// Tests for quantity normalisation, the only real logic in the package and the last local line of risk control.
/// </summary>
[TestClass]
public sealed class SymbolInfoQuantityTests
{
    [TestMethod]
    [DataRow("0.0019", "0.001")]
    [DataRow("0.0010", "0.001")]
    [DataRow("1.2345", "1.234")]
    [DataRow("0.9999", "0.999")]
    [DataRow("12.000999", "12")]
    public void 數量一律向下對齊到步進值(string input, string expected)
    {
        var symbol = TestSymbols.Btc() with { MinNotional = 0m };

        var result = symbol.NormalizeQuantity(TestSymbols.D(input));

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.AreEqual(TestSymbols.D(expected), normalized);
    }

    [TestMethod]
    [DataRow("0.0019")]
    [DataRow("1.2345")]
    [DataRow("999.9999")]
    [DataRow("0.001")]
    public void 校正後的數量不會超過原值且必定對齊(string input)
    {
        var symbol = TestSymbols.Btc() with { MinNotional = 0m };
        var requested = TestSymbols.D(input);

        var result = symbol.NormalizeQuantity(requested);

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.IsLessThanOrEqualTo(requested, normalized, $"{normalized} 不可大於 {requested}");
        Assert.IsTrue(symbol.IsQuantityAligned(normalized), $"{normalized} 未對齊步進值");
    }

    [TestMethod]
    public void 恰好等於最小下單量時通過()
    {
        var symbol = TestSymbols.Btc() with { MinQuantity = 0.005m, MinNotional = 0m };

        var result = symbol.NormalizeQuantity(0.005m);

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.AreEqual(0.005m, normalized);
    }

    [TestMethod]
    public void 比最小下單量少一個步進時回報失敗()
    {
        var symbol = TestSymbols.Btc() with { MinQuantity = 0.005m, MinNotional = 0m };

        var result = symbol.NormalizeQuantity(0.004m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.QuantityBelowMinimum, result.Error!.Code);
        Assert.AreEqual(ErrorCategory.Validation, result.Error.Category);
    }

    [TestMethod]
    public void 數量小於一個步進時向下對齊成零並回報失敗()
    {
        var symbol = TestSymbols.Btc() with { MinQuantity = 0m, MinNotional = 0m };

        var result = symbol.NormalizeQuantity(0.0009m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.QuantityBelowMinimum, result.Error!.Code);
    }

    [TestMethod]
    public void 最小下單量為零時真正的下限是步進值()
    {
        var symbol = TestSymbols.Btc() with { MinQuantity = 0m, MinNotional = 0m };

        Assert.AreEqual(symbol.StepSize, symbol.MinimumTradableQuantity);
        Assert.IsTrue(symbol.NormalizeQuantity(0.001m).IsSuccess);
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("-0.5")]
    public void 非正數的數量直接回報不合法(string input)
    {
        var symbol = TestSymbols.Btc();

        var result = symbol.NormalizeQuantity(TestSymbols.D(input));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidQuantity, result.Error!.Code);
    }

    [TestMethod]
    public void 超過單筆上限時回報失敗而不是截到上限()
    {
        var symbol = TestSymbols.Btc() with { MinNotional = 0m };

        var result = symbol.NormalizeQuantity(1000.001m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.QuantityAboveMaximum, result.Error!.Code);
    }

    [TestMethod]
    public void 恰好等於單筆上限時通過()
    {
        var symbol = TestSymbols.Btc() with { MinNotional = 0m };

        var result = symbol.NormalizeQuantity(1000m);

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.AreEqual(1000m, normalized);
    }

    [TestMethod]
    public void 名目價值恰好等於下限時通過()
    {
        var symbol = TestSymbols.Btc();

        var result = symbol.NormalizeQuantity(0.002m, 50_000m);

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.AreEqual(0.002m, normalized);
    }

    [TestMethod]
    public void 名目價值比下限少一個步進時回報失敗()
    {
        var symbol = TestSymbols.Btc();

        var result = symbol.NormalizeQuantity(0.001m, 50_000m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.NotionalBelowMinimum, result.Error!.Code);
        StringAssert.Contains(result.Error.Message, "50");
    }

    [TestMethod]
    public void 名目價值檢查用的參考價必須為正數()
    {
        var symbol = TestSymbols.Btc();

        var result = symbol.NormalizeQuantity(1m, 0m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidPrice, result.Error!.Code);
    }

    [TestMethod]
    public void 數量本身不合格時不會走到名目價值檢查()
    {
        var symbol = TestSymbols.Btc();

        var result = symbol.NormalizeQuantity(0.0001m, 50_000m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.QuantityBelowMinimum, result.Error!.Code);
    }

    [TestMethod]
    [DataRow("50000")]
    [DataRow("30000")]
    [DataRow("1234.5")]
    [DataRow("0.0001")]
    public void 算出來的最小數量一定通過校正(string priceText)
    {
        var symbol = TestSymbols.Btc() with { MinPrice = 0.00001m, MaxQuantity = 100_000_000m };
        var price = TestSymbols.D(priceText);

        var minimum = symbol.GetMinimumQuantity(price);

        Assert.IsTrue(minimum.TryGetValue(out var quantity), minimum.Error?.ToString());
        Assert.IsTrue(symbol.IsQuantityAligned(quantity));
        Assert.IsGreaterThanOrEqualTo(symbol.MinNotional, quantity * price, $"名目價值 {quantity * price} 不足 {symbol.MinNotional}");
        Assert.IsTrue(symbol.NormalizeQuantity(quantity, price).IsSuccess);
    }

    [TestMethod]
    public void 最小數量在名目價值除不盡時向上進位()
    {
        var symbol = TestSymbols.Btc();

        var result = symbol.GetMinimumQuantity(30_000m);

        Assert.IsTrue(result.TryGetValue(out var quantity), result.Error?.ToString());
        Assert.AreEqual(0.004m, quantity);
    }

    [TestMethod]
    public void 最小數量由最小下單量決定時不受名目價值影響()
    {
        var symbol = TestSymbols.Btc() with { MinQuantity = 0.5m, MinNotional = 0m };

        var result = symbol.GetMinimumQuantity(50_000m);

        Assert.IsTrue(result.TryGetValue(out var quantity), result.Error?.ToString());
        Assert.AreEqual(0.5m, quantity);
    }

    [TestMethod]
    public void 最小數量超過單筆上限時回報失敗()
    {
        var symbol = TestSymbols.Btc() with { MinNotional = 10_000_000m, MaxQuantity = 1m };

        var result = symbol.GetMinimumQuantity(1m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.QuantityAboveMaximum, result.Error!.Code);
    }

    [TestMethod]
    public void 最小數量的價格必須為正數()
    {
        var result = TestSymbols.Btc().GetMinimumQuantity(-1m);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidPrice, result.Error!.Code);
    }
}
