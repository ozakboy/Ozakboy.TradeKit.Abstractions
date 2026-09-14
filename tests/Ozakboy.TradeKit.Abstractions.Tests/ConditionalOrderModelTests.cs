namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 條件單模型、狀態列舉與識別碼的測試。
/// Tests for the conditional order model, its status enumeration, and its identifier.
/// </summary>
[TestClass]
public sealed class ConditionalOrderModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    private static ConditionalOrder Stop() => new()
    {
        Symbol = "BTCUSDT",
        ClientConditionalOrderId = "pt-stop-1",
        ExchangeConditionalOrderId = "991",
        Side = OrderSide.Sell,
        ConditionalOrderType = ConditionalOrderType.StopMarket,
        Status = ConditionalOrderStatus.New,
        Quantity = 0.01m,
        TriggerPrice = 48_000m,
        ReduceOnly = true,
        CreatedAt = Now,
        UpdatedAt = Now,
    };

    [TestMethod]
    public void 等待觸發與觸發中與已觸發都算仍然有效()
    {
        Assert.IsTrue(ConditionalOrderStatus.New.IsOpen());
        Assert.IsTrue(ConditionalOrderStatus.Triggering.IsOpen(), "送往撮合引擎的路上,還可能被打回來");
        Assert.IsTrue(ConditionalOrderStatus.Triggered.IsOpen(), "觸發後的委託還沒成交完,緊急出場仍需撤掉它");
        Assert.IsFalse(ConditionalOrderStatus.Filled.IsOpen());
        Assert.IsFalse(ConditionalOrderStatus.Finished.IsOpen());
        Assert.IsFalse(ConditionalOrderStatus.Canceled.IsOpen());
        Assert.IsFalse(ConditionalOrderStatus.Unspecified.IsOpen());
    }

    [TestMethod]
    public void 終態判定涵蓋成交結束撤銷失效與拒絕()
    {
        Assert.IsTrue(ConditionalOrderStatus.Filled.IsFinal());
        Assert.IsTrue(ConditionalOrderStatus.Finished.IsFinal());
        Assert.IsTrue(ConditionalOrderStatus.Canceled.IsFinal());
        Assert.IsTrue(ConditionalOrderStatus.Expired.IsFinal());
        Assert.IsTrue(ConditionalOrderStatus.Rejected.IsFinal());
        Assert.IsFalse(ConditionalOrderStatus.New.IsFinal());
        Assert.IsFalse(ConditionalOrderStatus.Triggering.IsFinal());
        Assert.IsFalse(ConditionalOrderStatus.Triggered.IsFinal());
    }

    [TestMethod]
    public void 結束與成交是兩個不同的終態()
    {
        // Finished 是誠實的「不知道成交還是被撤」。把它當成 Filled 的同義詞,
        // 會讓一張觸發後被撤掉的停損在帳上變成一次不存在的平倉。
        var terminal = Enum.GetValues<ConditionalOrderStatus>().Where(status => status.IsFinal()).ToList();

        Assert.Contains(ConditionalOrderStatus.Filled, terminal);
        Assert.Contains(ConditionalOrderStatus.Finished, terminal);
        Assert.IsTrue(ConditionalOrderStatus.Finished.IsFinal());
        Assert.IsFalse(ConditionalOrderStatus.Finished.IsOpen());
    }

    [TestMethod]
    public void 未指定狀態既不算有效也不算終態()
    {
        // 對映漏接的狀態必須兩邊都不落腳,否則會被靜默當成一個合理的狀態。
        Assert.IsFalse(ConditionalOrderStatus.Unspecified.IsOpen());
        Assert.IsFalse(ConditionalOrderStatus.Unspecified.IsFinal());
    }

    [TestMethod]
    public void 列舉的零值是未指定()
    {
        // 忘記賦值的欄位不可以落在任何一個真的會送出去的值上。
        var types = Enum.GetValues<ConditionalOrderType>();
        var statuses = Enum.GetValues<ConditionalOrderStatus>();

        Assert.AreEqual(ConditionalOrderType.Unspecified, types[0]);
        Assert.AreEqual(0, (int)types[0]);
        Assert.AreEqual(ConditionalOrderStatus.Unspecified, statuses[0]);
        Assert.AreEqual(0, (int)statuses[0]);
    }

    [TestMethod]
    public void 只有限價類型會在觸發後掛限價單()
    {
        Assert.IsTrue(ConditionalOrderType.StopLimit.PlacesLimitOrder());
        Assert.IsTrue(ConditionalOrderType.TakeProfitLimit.PlacesLimitOrder());
        Assert.IsFalse(ConditionalOrderType.StopMarket.PlacesLimitOrder());
        Assert.IsFalse(ConditionalOrderType.TakeProfitMarket.PlacesLimitOrder());
        Assert.IsFalse(ConditionalOrderType.TrailingStopMarket.PlacesLimitOrder());
        Assert.IsFalse(ConditionalOrderType.Unspecified.PlacesLimitOrder());
    }

    [TestMethod]
    public void 條件單類型對映得回一般委託類型()
    {
        Assert.AreEqual(OrderType.StopMarket, ConditionalOrderType.StopMarket.ToOrderType());
        Assert.AreEqual(OrderType.StopLimit, ConditionalOrderType.StopLimit.ToOrderType());
        Assert.AreEqual(OrderType.TakeProfitMarket, ConditionalOrderType.TakeProfitMarket.ToOrderType());
        Assert.AreEqual(OrderType.TakeProfitLimit, ConditionalOrderType.TakeProfitLimit.ToOrderType());
        Assert.AreEqual(OrderType.TrailingStopMarket, ConditionalOrderType.TrailingStopMarket.ToOrderType());
        Assert.AreEqual(OrderType.Unspecified, ConditionalOrderType.Unspecified.ToOrderType());
    }

    [TestMethod]
    public void 有交易所編號時識別碼優先用它()
    {
        var identifier = Stop().GetIdentifier();

        Assert.AreEqual("991", identifier.ExchangeConditionalOrderId);
        Assert.IsNull(identifier.ClientConditionalOrderId);
    }

    [TestMethod]
    public void 沒有交易所編號時識別碼退回用戶端編號()
    {
        var identifier = (Stop() with { ExchangeConditionalOrderId = null }).GetIdentifier();

        Assert.AreEqual("pt-stop-1", identifier.ClientConditionalOrderId);
        Assert.IsNull(identifier.ExchangeConditionalOrderId);
    }

    [TestMethod]
    public void 預設的條件單識別碼是空的()
    {
        var identifier = default(ConditionalOrderIdentifier);

        Assert.IsTrue(identifier.IsEmpty);
        Assert.AreEqual("(empty)", identifier.ToString());
    }

    [TestMethod]
    public void 條件單識別碼拒絕空白編號()
    {
        Assert.ThrowsExactly<ArgumentException>(() => ConditionalOrderIdentifier.FromExchangeId("  "));
        Assert.ThrowsExactly<ArgumentException>(() => ConditionalOrderIdentifier.FromClientId("  "));
    }

    [TestMethod]
    public void 條件單識別碼的描述指出用的是哪一種編號()
    {
        Assert.AreEqual("conditionalOrderId=991", ConditionalOrderIdentifier.FromExchangeId("991").ToString());
        Assert.AreEqual(
            "clientConditionalOrderId=pt-stop-1",
            ConditionalOrderIdentifier.FromClientId("pt-stop-1").ToString());
    }

    [TestMethod]
    public void 尚未觸發的條件單沒有實際委託編號與觸發時間()
    {
        var stop = Stop();

        Assert.IsNull(stop.TriggeredOrderId);
        Assert.IsNull(stop.TriggeredAt);
        Assert.IsTrue(stop.IsOpen);
        Assert.IsFalse(stop.IsFinal);
    }

    [TestMethod]
    public void 觸發之後才接得回那張實際送出的委託()
    {
        var triggered = Stop() with
        {
            Status = ConditionalOrderStatus.Triggered,
            TriggeredOrderId = "4477",
            TriggeredAt = Now.AddMinutes(3),
            UpdatedAt = Now.AddMinutes(3),
        };

        Assert.AreEqual("4477", triggered.TriggeredOrderId);
        Assert.AreEqual(Now.AddMinutes(3), triggered.TriggeredAt);
        Assert.IsTrue(triggered.IsOpen);
    }

    [TestMethod]
    public void 條件單更新帶得出原始狀態與拒絕原因()
    {
        var update = new ConditionalOrderUpdate
        {
            ConditionalOrder = Stop() with { Status = ConditionalOrderStatus.Rejected },
            RawStatus = "REJECTED",
            RejectReason = "Order would immediately trigger.",
            Timestamp = Now,
        };

        Assert.AreEqual(ConditionalOrderStatus.Rejected, update.ConditionalOrder.Status);
        Assert.AreEqual("REJECTED", update.RawStatus);
        Assert.AreEqual("Order would immediately trigger.", update.RejectReason);
        Assert.AreEqual(Now, update.Timestamp);
    }

    [TestMethod]
    public void 找不到條件單的錯誤與找不到委託分屬不同代碼()
    {
        var identifier = ConditionalOrderIdentifier.FromClientId("pt-stop-1");

        var error = TradeErrors.ConditionalOrderNotFound(identifier);

        Assert.AreEqual(TradeErrorCodes.ConditionalOrderNotFound, error.Code);
        Assert.AreNotEqual(TradeErrorCodes.OrderNotFound, error.Code);
        Assert.Contains("pt-stop-1", error.Message);
    }

    [TestMethod]
    public void 條件單走錯路徑的錯誤指名該改呼叫哪一個方法()
    {
        var error = TradeErrors.ConditionalOrderPathRequired(OrderType.StopMarket);

        Assert.AreEqual(TradeErrorCodes.ConditionalOrderPathRequired, error.Code);
        Assert.Contains("PlaceConditionalOrderAsync", error.Message);
    }
}
