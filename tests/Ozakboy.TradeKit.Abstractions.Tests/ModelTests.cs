namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 各模型的衍生欄位與查詢方法測試。
/// Tests for the derived members and lookup methods on the models.
/// </summary>
[TestClass]
public sealed class ModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static Order NewOrder() => new()
    {
        Symbol = "BTCUSDT",
        ClientOrderId = "pt-0001",
        Side = OrderSide.Buy,
        OrderType = OrderType.Limit,
        Status = OrderStatus.PartiallyFilled,
        Quantity = 1m,
        FilledQuantity = 0.25m,
        AverageFillPrice = 50_000m,
        Price = 50_000m,
        CreatedAt = Now,
        UpdatedAt = Now,
    };

    [TestMethod]
    public void 委託的剩餘數量等於委託量減已成交量()
    {
        Assert.AreEqual(0.75m, NewOrder().RemainingQuantity);
    }

    [TestMethod]
    public void 部分成交仍算在簿上()
    {
        var order = NewOrder();

        Assert.IsTrue(order.IsOpen);
        Assert.IsFalse(order.IsFinal);
    }

    [TestMethod]
    public void 撤單確認前仍算在簿上()
    {
        var order = NewOrder() with { Status = OrderStatus.PendingCancel };

        Assert.IsTrue(order.IsOpen);
        Assert.IsFalse(order.IsFinal);
    }

    [TestMethod]
    [DataRow(OrderStatus.Filled)]
    [DataRow(OrderStatus.Canceled)]
    [DataRow(OrderStatus.Rejected)]
    [DataRow(OrderStatus.Expired)]
    public void 終態的委託不再變化(OrderStatus status)
    {
        var order = NewOrder() with { Status = status };

        Assert.IsTrue(order.IsFinal);
        Assert.IsFalse(order.IsOpen);
    }

    [TestMethod]
    public void 未指定狀態既不在簿上也不是終態()
    {
        Assert.IsFalse(OrderStatus.Unspecified.IsOpen());
        Assert.IsFalse(OrderStatus.Unspecified.IsFinal());
    }

    [TestMethod]
    public void 委託識別碼優先使用交易所編號()
    {
        var withExchangeId = NewOrder() with { ExchangeOrderId = "88991" };

        Assert.AreEqual("88991", withExchangeId.GetIdentifier().ExchangeOrderId);
        Assert.AreEqual("pt-0001", NewOrder().GetIdentifier().ClientOrderId);
    }

    [TestMethod]
    public void 識別碼只會帶一種編號()
    {
        var byExchange = OrderIdentifier.FromExchangeId("88991");
        var byClient = OrderIdentifier.FromClientId("pt-0001");

        Assert.IsNull(byExchange.ClientOrderId);
        Assert.IsNull(byClient.ExchangeOrderId);
        Assert.IsFalse(byExchange.IsEmpty);
        Assert.IsFalse(byClient.IsEmpty);
        StringAssert.Contains(byExchange.ToString(), "88991");
        StringAssert.Contains(byClient.ToString(), "pt-0001");
    }

    [TestMethod]
    public void 預設的識別碼是空的()
    {
        var empty = default(OrderIdentifier);

        Assert.IsTrue(empty.IsEmpty);
        Assert.AreEqual("(empty)", empty.ToString());
    }

    [TestMethod]
    public void 識別碼不接受空白編號()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() => OrderIdentifier.FromExchangeId("  "));
        _ = Assert.ThrowsExactly<ArgumentException>(() => OrderIdentifier.FromClientId(""));
    }

    [TestMethod]
    public void 多單的數量為正空單為負()
    {
        var longPosition = new Position { Symbol = "BTCUSDT", Quantity = 1.5m, MarkPrice = 50_000m };
        var shortPosition = longPosition with { Quantity = -1.5m };

        Assert.IsTrue(longPosition.IsLong);
        Assert.IsFalse(longPosition.IsShort);
        Assert.IsTrue(shortPosition.IsShort);
        Assert.AreEqual(1.5m, shortPosition.AbsoluteQuantity);
        Assert.AreEqual(75_000m, shortPosition.Notional);
        Assert.AreEqual(OrderSide.Sell, longPosition.ClosingSide);
        Assert.AreEqual(OrderSide.Buy, shortPosition.ClosingSide);
    }

    [TestMethod]
    public void 空手的持倉沒有平倉方向()
    {
        var flat = Position.Flat("BTCUSDT", Now);

        Assert.IsTrue(flat.IsFlat);
        Assert.IsFalse(flat.IsLong);
        Assert.IsFalse(flat.IsShort);
        Assert.AreEqual(OrderSide.Unspecified, flat.ClosingSide);
        Assert.AreEqual(Now, flat.UpdatedAt);
        Assert.AreEqual(PositionSide.Both, flat.Side);
    }

    [TestMethod]
    public void 空手持倉不接受空白代碼()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() => Position.Flat(" ", Now));
    }

    [TestMethod]
    public void 保證金餘額含未實現損益()
    {
        var balance = new Balance
        {
            Asset = "USDT",
            WalletBalance = 1_000m,
            AvailableBalance = 400m,
            UnrealizedPnl = -120m,
        };

        Assert.AreEqual(880m, balance.MarginBalance);
        Assert.AreEqual(new Money(1_000m, "USDT"), balance.ToMoney());
        Assert.AreEqual(new Money(400m, "USDT"), balance.AvailableAsMoney());
    }

    [TestMethod]
    public void 未提供的保證金需求是未知而不是零()
    {
        // 缺值若預設成 0,風控會讀成「沒有維持保證金需求」,也就是沒有強平風險。
        // A missing value defaulting to zero would read as "no maintenance margin required" — no liquidation risk.
        var balance = new Balance { Asset = "USDT", WalletBalance = 1_000m };
        var snapshot = new AccountSnapshot { Balances = [balance], TakenAt = Now };

        Assert.IsNull(balance.MaintenanceMargin);
        Assert.IsNull(balance.InitialMargin);
        Assert.IsNull(snapshot.TotalMaintenanceMargin);
        Assert.IsNull(snapshot.TotalInitialMargin);
    }

    [TestMethod]
    public void 交易所明確給的零保證金需求保留為零()
    {
        var balance = new Balance { Asset = "USDT", MaintenanceMargin = 0m, InitialMargin = 0m };
        var snapshot = new AccountSnapshot { TotalMaintenanceMargin = 0m, TotalInitialMargin = 0m, TakenAt = Now };

        Assert.AreEqual(0m, balance.MaintenanceMargin);
        Assert.AreEqual(0m, balance.InitialMargin);
        Assert.AreEqual(0m, snapshot.TotalMaintenanceMargin);
        Assert.AreEqual(0m, snapshot.TotalInitialMargin);
    }

    [TestMethod]
    public void 維持保證金可以算出保證金率()
    {
        var balance = new Balance
        {
            Asset = "USDT",
            WalletBalance = 1_000m,
            UnrealizedPnl = -100m,
            MaintenanceMargin = 450m,
            InitialMargin = 900m,
        };

        Assert.AreEqual(450m, balance.MaintenanceMargin);
        Assert.AreEqual(900m, balance.InitialMargin);
        Assert.AreEqual(200m, balance.MarginBalance / balance.MaintenanceMargin!.Value * 100m);
    }

    [TestMethod]
    public void 帳戶快照可以查餘額與持倉()
    {
        var snapshot = new AccountSnapshot
        {
            Balances = [new Balance { Asset = "USDT", WalletBalance = 1_000m }],
            Positions = [new Position { Symbol = "BTCUSDT", Quantity = 1m }],
            TakenAt = Now,
        };

        Assert.IsTrue(snapshot.GetBalance("usdt").TryGetValue(out var balance));
        Assert.AreEqual(1_000m, balance.WalletBalance);
        Assert.IsTrue(snapshot.GetPosition("btcusdt").TryGetValue(out var position));
        Assert.AreEqual(1m, position.Quantity);
        Assert.IsTrue(snapshot.CanTrade);
        Assert.IsFalse(snapshot.IsHedgeMode);
    }

    [TestMethod]
    public void 查不到的餘額與持倉回報明確錯誤()
    {
        var snapshot = new AccountSnapshot { TakenAt = Now };

        Assert.AreEqual(TradeErrorCodes.BalanceNotFound, snapshot.GetBalance("USDT").Error!.Code);
        Assert.AreEqual(TradeErrorCodes.PositionNotFound, snapshot.GetPosition("BTCUSDT").Error!.Code);
    }

    [TestMethod]
    public void 查不到持倉時可以取得空手的持倉()
    {
        var snapshot = new AccountSnapshot { TakenAt = Now };

        var position = snapshot.GetPositionOrFlat("BTCUSDT");

        Assert.IsTrue(position.IsFlat);
        Assert.AreEqual("BTCUSDT", position.Symbol);
        Assert.AreEqual(Now, position.UpdatedAt);
    }

    [TestMethod]
    public void 帳戶快照的查詢不接受空參數()
    {
        var snapshot = new AccountSnapshot { TakenAt = Now };

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => snapshot.GetBalance(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => snapshot.GetPosition(null!));
        _ = Assert.ThrowsExactly<ArgumentException>(() => snapshot.GetPositionOrFlat(" "));
    }

    [TestMethod]
    public void K線的衍生欄位()
    {
        var kline = new Kline
        {
            Symbol = "BTCUSDT",
            Interval = KlineInterval.FifteenMinutes,
            OpenTime = Now,
            CloseTime = Now.AddMinutes(15),
            Open = 100m,
            High = 130m,
            Low = 70m,
            Close = 120m,
            IsClosed = true,
        };

        Assert.AreEqual(60m, kline.Range);
        Assert.IsTrue(kline.IsBullish);
        Assert.AreEqual((130m + 70m + 120m) / 3m, kline.TypicalPrice);
        Assert.IsFalse((kline with { Close = 90m }).IsBullish);
    }

    [TestMethod]
    public void 成交明細的手續費帶幣別()
    {
        var trade = new Trade
        {
            Symbol = "BTCUSDT",
            TradeId = "1",
            Side = OrderSide.Buy,
            Price = 50_000m,
            Quantity = 0.01m,
            Fee = 0.2m,
            FeeAsset = "BNB",
            ExecutedAt = Now,
        };

        Assert.AreEqual(500m, trade.Notional);
        Assert.AreEqual(new Money(0.2m, "BNB"), trade.FeeAsMoney());
        Assert.AreEqual(PositionSide.Both, trade.PositionSide);
    }

    [TestMethod]
    public void 標記價更新保留資金費率()
    {
        var update = new MarkPriceUpdate
        {
            Symbol = "BTCUSDT",
            MarkPrice = 50_000m,
            IndexPrice = 49_990m,
            FundingRate = 0.0001m,
            NextFundingTime = Now.AddHours(4),
            Timestamp = Now,
        };

        Assert.AreEqual(50_000m, update.MarkPrice);
        Assert.AreEqual(0.0001m, update.FundingRate);
        Assert.AreEqual(Now.AddHours(4), update.NextFundingTime);
    }

    [TestMethod]
    public void K線查詢條件的驗證()
    {
        var query = new KlineQuery { Symbol = "BTCUSDT", Interval = KlineInterval.OneHour };

        Assert.IsTrue(query.Validate().IsSuccess);
        Assert.AreEqual(TradeErrorCodes.InvalidQuery, (query with { Symbol = " " }).Validate().Error!.Code);
        Assert.AreEqual(
            TradeErrorCodes.UnsupportedInterval,
            (query with { Interval = KlineInterval.Unspecified }).Validate().Error!.Code);
        Assert.AreEqual(
            TradeErrorCodes.InvalidQuery,
            (query with { StartTime = Now, EndTime = Now }).Validate().Error!.Code);
        Assert.AreEqual(TradeErrorCodes.InvalidQuery, (query with { Limit = 0 }).Validate().Error!.Code);
        Assert.IsTrue((query with { StartTime = Now, EndTime = Now.AddHours(1), Limit = 500 }).Validate().IsSuccess);
    }
}
