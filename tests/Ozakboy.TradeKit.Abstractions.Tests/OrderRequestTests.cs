namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 下單請求的欄位驗證與交易規則校正測試。
/// Tests for order request validation and trading-rule normalisation.
/// </summary>
[TestClass]
public sealed class OrderRequestTests
{
    private static OrderRequest Limit() => new()
    {
        Symbol = "BTCUSDT",
        Side = OrderSide.Buy,
        OrderType = OrderType.Limit,
        Quantity = 0.01m,
        Price = 50_000m,
    };

    private static OrderRequest Market() => new()
    {
        Symbol = "BTCUSDT",
        Side = OrderSide.Sell,
        OrderType = OrderType.Market,
        Quantity = 0.01m,
    };

    [TestMethod]
    public void 合法的限價單通過驗證()
    {
        Assert.IsTrue(Limit().Validate().IsSuccess);
    }

    [TestMethod]
    public void 合法的市價單通過驗證()
    {
        Assert.IsTrue(Market().Validate().IsSuccess);
    }

    [TestMethod]
    public void 停損市價單需要觸發價且不得帶價格()
    {
        var request = Market() with { OrderType = OrderType.StopMarket, StopPrice = 48_000m };

        Assert.IsTrue(request.Validate().IsSuccess);
        Assert.IsTrue((request with { StopPrice = null }).Validate().IsFailure);
        Assert.IsTrue((request with { Price = 48_000m }).Validate().IsFailure);
    }

    [TestMethod]
    public void 停損限價單同時需要價格與觸發價()
    {
        var request = Limit() with { OrderType = OrderType.StopLimit, StopPrice = 48_000m };

        Assert.IsTrue(request.Validate().IsSuccess);
        Assert.IsTrue((request with { StopPrice = null }).Validate().IsFailure);
        Assert.IsTrue((request with { Price = null }).Validate().IsFailure);
    }

    [TestMethod]
    public void 移動停損需要合理的回撤比例()
    {
        var request = Market() with { OrderType = OrderType.TrailingStopMarket, CallbackRate = 1.5m };

        Assert.IsTrue(request.Validate().IsSuccess);
        Assert.IsTrue((request with { CallbackRate = null }).Validate().IsFailure);
        Assert.IsTrue((request with { CallbackRate = 0m }).Validate().IsFailure);
        Assert.IsTrue((request with { CallbackRate = 100.1m }).Validate().IsFailure);
    }

    [TestMethod]
    public void 非移動停損不得指定回撤比例()
    {
        var result = (Limit() with { CallbackRate = 1m }).Validate();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidOrderRequest, result.Error!.Code);
    }

    [TestMethod]
    public void 市價單不得指定價格()
    {
        var result = (Market() with { Price = 50_000m }).Validate();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidOrderRequest, result.Error!.Code);
    }

    [TestMethod]
    public void 限價單的價格必須大於零()
    {
        Assert.IsTrue((Limit() with { Price = 0m }).Validate().IsFailure);
        Assert.IsTrue((Limit() with { Price = -1m }).Validate().IsFailure);
    }

    [TestMethod]
    public void 限價單必須指定有效期限規則()
    {
        var result = (Limit() with { TimeInForce = TimeInForce.Unspecified }).Validate();

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void 未指定方向或類型一律擋下()
    {
        Assert.IsTrue((Limit() with { Side = OrderSide.Unspecified }).Validate().IsFailure);
        Assert.IsTrue((Limit() with { OrderType = OrderType.Unspecified }).Validate().IsFailure);
    }

    [TestMethod]
    public void 交易對代碼不可為空白()
    {
        Assert.IsTrue((Limit() with { Symbol = "   " }).Validate().IsFailure);
    }

    [TestMethod]
    public void 數量必須大於零()
    {
        Assert.IsTrue((Limit() with { Quantity = 0m }).Validate().IsFailure);
        Assert.IsTrue((Limit() with { Quantity = -1m }).Validate().IsFailure);
    }

    [TestMethod]
    public void 全部平倉不可同時指定數量或只減倉()
    {
        var closeAll = Market() with { ClosePosition = true, Quantity = 0m };

        Assert.IsTrue(closeAll.Validate().IsSuccess);
        Assert.IsTrue((closeAll with { Quantity = 1m }).Validate().IsFailure);
        Assert.IsTrue((closeAll with { ReduceOnly = true }).Validate().IsFailure);
    }

    [TestMethod]
    public void 校正會同時對齊價格與數量()
    {
        var request = Limit() with { Price = 50_000.16m, Quantity = 0.0025m };

        var result = request.NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.AreEqual(50_000.2m, normalized.Price);
        Assert.AreEqual(0.002m, normalized.Quantity);
        Assert.AreEqual(0.0025m, request.Quantity, "原請求不可被改動");
    }

    [TestMethod]
    public void 校正會一併對齊觸發價()
    {
        var request = Market() with { OrderType = OrderType.StopMarket, StopPrice = 48_000.17m, Quantity = 0.01m };

        var result = request.NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.AreEqual(48_000.2m, normalized.StopPrice);
    }

    [TestMethod]
    public void 市價單沒有價格時只對齊數量()
    {
        var request = Market() with { Quantity = 0.0025m };

        var result = request.NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.AreEqual(0.002m, normalized.Quantity);
        Assert.IsNull(normalized.Price);
    }

    [TestMethod]
    public void 校正時會擋下名目價值不足的委託()
    {
        var request = Limit() with { Quantity = 0.001m, Price = 50_000m };

        var result = request.NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.NotionalBelowMinimum, result.Error!.Code);
    }

    [TestMethod]
    public void 校正時會擋下欄位組合不合法的委託()
    {
        var result = (Limit() with { Price = null }).NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidOrderRequest, result.Error!.Code);
    }

    [TestMethod]
    public void 交易規則屬於別的商品時拒絕校正()
    {
        var symbol = TestSymbols.Btc() with { Name = "ETHUSDT" };

        var result = Limit().NormalizeFor(symbol);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidOrderRequest, result.Error!.Code);
    }

    [TestMethod]
    public void 商品停止交易時拒絕校正()
    {
        var symbol = TestSymbols.Btc() with { IsTradingEnabled = false };

        var result = Limit().NormalizeFor(symbol);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.SymbolNotTradable, result.Error!.Code);
    }

    [TestMethod]
    public void 全部平倉的委託不需要校正數量()
    {
        var request = Market() with { ClosePosition = true, Quantity = 0m };

        var result = request.NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.AreEqual(0m, normalized.Quantity);
    }

    [TestMethod]
    public void 價格對齊方向可以指定()
    {
        var request = Limit() with { Price = 50_000.19m, Quantity = 0.01m };

        var down = request.NormalizeFor(TestSymbols.Btc(), PriceRounding.Down);
        var up = request.NormalizeFor(TestSymbols.Btc(), PriceRounding.Up);

        Assert.IsTrue(down.TryGetValue(out var lower));
        Assert.IsTrue(up.TryGetValue(out var higher));
        Assert.AreEqual(50_000.1m, lower.Price);
        Assert.AreEqual(50_000.2m, higher.Price);
    }

    [TestMethod]
    public void 交易規則為空時視為程式缺陷()
    {
        var request = Limit();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => request.NormalizeFor(null!));
    }

    [TestMethod]
    public void 預設值採單向模式與標記價觸發()
    {
        var request = Limit();

        Assert.AreEqual(PositionSide.Both, request.PositionSide);
        Assert.AreEqual(TriggerPriceType.MarkPrice, request.TriggerPriceType);
        Assert.AreEqual(TimeInForce.GoodTilCanceled, request.TimeInForce);
        Assert.IsNull(request.ClientOrderId);
    }
}
