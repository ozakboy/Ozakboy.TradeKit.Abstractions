namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 條件單請求的欄位驗證與交易規則校正測試。
/// Tests for conditional order request validation and trading-rule normalisation.
/// </summary>
[TestClass]
public sealed class ConditionalOrderRequestTests
{
    private static ConditionalOrderRequest StopMarket() => new()
    {
        Symbol = "BTCUSDT",
        Side = OrderSide.Sell,
        ConditionalOrderType = ConditionalOrderType.StopMarket,
        Quantity = 0.01m,
        TriggerPrice = 48_000m,
        ReduceOnly = true,
    };

    private static ConditionalOrderRequest StopLimit() => new()
    {
        Symbol = "BTCUSDT",
        Side = OrderSide.Sell,
        ConditionalOrderType = ConditionalOrderType.StopLimit,
        Quantity = 0.01m,
        TriggerPrice = 48_000m,
        Price = 47_900m,
        ReduceOnly = true,
    };

    private static ConditionalOrderRequest Trailing() => new()
    {
        Symbol = "BTCUSDT",
        Side = OrderSide.Sell,
        ConditionalOrderType = ConditionalOrderType.TrailingStopMarket,
        Quantity = 0.01m,
        CallbackRate = 1.5m,
        ReduceOnly = true,
    };

    [TestMethod]
    public void 合法的停損市價單通過驗證()
    {
        Assert.IsTrue(StopMarket().Validate().IsSuccess);
    }

    [TestMethod]
    public void 合法的停損限價單通過驗證()
    {
        Assert.IsTrue(StopLimit().Validate().IsSuccess);
    }

    [TestMethod]
    public void 合法的移動停損通過驗證()
    {
        Assert.IsTrue(Trailing().Validate().IsSuccess);
    }

    [TestMethod]
    public void 未指定類型的條件單被擋下()
    {
        var result = (StopMarket() with { ConditionalOrderType = ConditionalOrderType.Unspecified }).Validate();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidOrderRequest, result.Error!.Code);
    }

    [TestMethod]
    public void 未指定方向的條件單被擋下()
    {
        Assert.IsTrue((StopMarket() with { Side = OrderSide.Unspecified }).Validate().IsFailure);
    }

    [TestMethod]
    public void 空白的交易對被擋下()
    {
        Assert.IsTrue((StopMarket() with { Symbol = "  " }).Validate().IsFailure);
    }

    [TestMethod]
    public void 市價類型的條件單必須有觸發價且不得帶委託價()
    {
        var request = StopMarket();

        Assert.IsTrue(request.Validate().IsSuccess);
        Assert.IsTrue((request with { TriggerPrice = null }).Validate().IsFailure);
        Assert.IsTrue((request with { TriggerPrice = 0m }).Validate().IsFailure);
        Assert.IsTrue((request with { Price = 47_900m }).Validate().IsFailure);
    }

    [TestMethod]
    public void 限價類型的條件單同時需要觸發價與委託價()
    {
        var request = StopLimit();

        Assert.IsTrue(request.Validate().IsSuccess);
        Assert.IsTrue((request with { TriggerPrice = null }).Validate().IsFailure);
        Assert.IsTrue((request with { Price = null }).Validate().IsFailure);
        Assert.IsTrue((request with { TimeInForce = TimeInForce.Unspecified }).Validate().IsFailure);
    }

    [TestMethod]
    public void 停利類型與停損類型套用同一組規則()
    {
        var takeProfitMarket = StopMarket() with { ConditionalOrderType = ConditionalOrderType.TakeProfitMarket };
        var takeProfitLimit = StopLimit() with { ConditionalOrderType = ConditionalOrderType.TakeProfitLimit };

        Assert.IsTrue(takeProfitMarket.Validate().IsSuccess);
        Assert.IsTrue(takeProfitLimit.Validate().IsSuccess);
        Assert.IsTrue((takeProfitMarket with { TriggerPrice = null }).Validate().IsFailure);
        Assert.IsTrue((takeProfitLimit with { Price = null }).Validate().IsFailure);
    }

    [TestMethod]
    public void 移動停損需要合理的回撤比例()
    {
        var request = Trailing();

        Assert.IsTrue(request.Validate().IsSuccess);
        Assert.IsTrue((request with { CallbackRate = null }).Validate().IsFailure);
        Assert.IsTrue((request with { CallbackRate = 0m }).Validate().IsFailure);
        Assert.IsTrue((request with { CallbackRate = 100.1m }).Validate().IsFailure);
        Assert.IsTrue((request with { CallbackRate = 100m }).Validate().IsSuccess);
    }

    [TestMethod]
    public void 移動停損不得自行指定觸發價()
    {
        var result = (Trailing() with { TriggerPrice = 48_000m }).Validate();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidOrderRequest, result.Error!.Code);
    }

    [TestMethod]
    public void 移動停損可以指定啟動價但必須大於零()
    {
        Assert.IsTrue((Trailing() with { ActivationPrice = 52_000m }).Validate().IsSuccess);
        Assert.IsTrue((Trailing() with { ActivationPrice = 0m }).Validate().IsFailure);
    }

    [TestMethod]
    public void 非移動停損不得指定回撤比例或啟動價()
    {
        Assert.IsTrue((StopMarket() with { CallbackRate = 1m }).Validate().IsFailure);
        Assert.IsTrue((StopMarket() with { ActivationPrice = 52_000m }).Validate().IsFailure);
    }

    [TestMethod]
    public void 全部平倉與只減倉不可同時指定()
    {
        var result = (StopMarket() with { Quantity = 0m, ClosePosition = true, ReduceOnly = true }).Validate();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidOrderRequest, result.Error!.Code);
    }

    [TestMethod]
    public void 全部平倉時不可指定數量()
    {
        var closeAll = StopMarket() with { ReduceOnly = false, ClosePosition = true, Quantity = 0m };

        Assert.IsTrue(closeAll.Validate().IsSuccess);
        Assert.IsTrue((closeAll with { Quantity = 0.01m }).Validate().IsFailure);
    }

    [TestMethod]
    public void 限價類型不支援全部平倉()
    {
        // 觸發後掛的是限價單,沒成交就等於沒平倉,「全部平倉」在這裡是一句做不到的承諾。
        var closeAll = StopLimit() with { ReduceOnly = false, ClosePosition = true, Quantity = 0m };

        var result = closeAll.Validate();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidOrderRequest, result.Error!.Code);
    }

    [TestMethod]
    public void 非全部平倉的數量必須大於零()
    {
        Assert.IsTrue((StopMarket() with { Quantity = 0m }).Validate().IsFailure);
        Assert.IsTrue((StopMarket() with { Quantity = -0.01m }).Validate().IsFailure);
    }

    [TestMethod]
    public void 用戶端條件單編號不可為空白字串()
    {
        Assert.IsTrue((StopMarket() with { ClientConditionalOrderId = "   " }).Validate().IsFailure);
        Assert.IsTrue((StopMarket() with { ClientConditionalOrderId = null }).Validate().IsSuccess);
        Assert.IsTrue((StopMarket() with { ClientConditionalOrderId = "pt-stop-1" }).Validate().IsSuccess);
    }

    [TestMethod]
    public void 觸發價預設看標記價()
    {
        Assert.AreEqual(TriggerPriceType.MarkPrice, StopMarket().TriggerPriceType);
    }

    [TestMethod]
    public void 校正會對齊觸發價委託價與數量()
    {
        var request = StopLimit() with { TriggerPrice = 48_000.17m, Price = 47_900.13m, Quantity = 0.0125m };

        var result = request.NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.AreEqual(48_000.2m, normalized.TriggerPrice);
        Assert.AreEqual(47_900.1m, normalized.Price);
        Assert.AreEqual(0.012m, normalized.Quantity, "數量一律向下對齊,否則實際部位會大於風控算出來的規模");
    }

    [TestMethod]
    public void 校正也會對齊移動停損的啟動價()
    {
        var request = Trailing() with { ActivationPrice = 52_000.17m };

        var result = request.NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.AreEqual(52_000.2m, normalized.ActivationPrice);
    }

    [TestMethod]
    public void 校正的取整方向由呼叫端決定()
    {
        var request = StopMarket() with { TriggerPrice = 48_000.17m };

        var down = request.NormalizeFor(TestSymbols.Btc(), PriceRounding.Down);
        var up = request.NormalizeFor(TestSymbols.Btc(), PriceRounding.Up);

        Assert.IsTrue(down.TryGetValue(out var roundedDown));
        Assert.IsTrue(up.TryGetValue(out var roundedUp));
        Assert.AreEqual(48_000.1m, roundedDown.TriggerPrice);
        Assert.AreEqual(48_000.2m, roundedUp.TriggerPrice);
    }

    [TestMethod]
    public void 校正時交易對規則必須與請求相符()
    {
        var result = (StopMarket() with { Symbol = "ETHUSDT" }).NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidOrderRequest, result.Error!.Code);
    }

    [TestMethod]
    public void 不可交易的商品無法校正()
    {
        var symbol = TestSymbols.Btc() with { IsTradingEnabled = false };

        var result = StopMarket().NormalizeFor(symbol);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.SymbolNotTradable, result.Error!.Code);
    }

    [TestMethod]
    public void 全部平倉的請求略過數量校正()
    {
        var closeAll = StopMarket() with { ReduceOnly = false, ClosePosition = true, Quantity = 0m };

        var result = closeAll.NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.TryGetValue(out var normalized), result.Error?.ToString());
        Assert.AreEqual(0m, normalized.Quantity);
        Assert.AreEqual(48_000m, normalized.TriggerPrice);
    }

    [TestMethod]
    public void 名目價值不足的條件單校正失敗()
    {
        // 觸發價 48000、數量 0.001 → 名目 48,低於 TestSymbols.Btc 的最小名目 100。
        var request = StopMarket() with { Quantity = 0.001m };

        var result = request.NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.NotionalBelowMinimum, result.Error!.Code);
    }

    [TestMethod]
    public void 欄位組合不合法時校正直接回報驗證失敗()
    {
        var result = (StopMarket() with { TriggerPrice = null }).NormalizeFor(TestSymbols.Btc());

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.InvalidOrderRequest, result.Error!.Code);
    }

    [TestMethod]
    public void 校正不會動到原請求()
    {
        var request = StopMarket() with { TriggerPrice = 48_000.17m };

        _ = request.NormalizeFor(TestSymbols.Btc());

        Assert.AreEqual(48_000.17m, request.TriggerPrice);
    }

    [TestMethod]
    public void 交易規則為null時擲出例外()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => StopMarket().NormalizeFor(null!));
    }
}
