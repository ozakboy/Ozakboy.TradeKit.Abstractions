namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 驗收測試:證明本套件的介面可以由一個純記憶體的實作完整滿足,不需要網路、WebSocket 或 API 金鑰。
/// The acceptance test: every interface here can be satisfied by a purely in-memory implementation, with no
/// network, WebSocket, or API key.
/// </summary>
[TestClass]
public sealed class InMemoryExchangeTests
{
    private static readonly DateTimeOffset Start = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    private static InMemoryExchange CreateExchange()
    {
        var symbol = TestSymbols.Btc();
        var klines = Enumerable.Range(0, 5).Select(index => new Kline
        {
            Symbol = symbol.Name,
            Interval = KlineInterval.FifteenMinutes,
            OpenTime = Start.AddMinutes(15 * index),
            CloseTime = Start.AddMinutes((15 * index) + 15),
            Open = 50_000m,
            High = 50_100m,
            Low = 49_900m,
            Close = 50_050m,
            Volume = 12m,
            IsClosed = true,
        });

        return new InMemoryExchange(symbol, klines, Start);
    }

    [TestMethod]
    public async Task 模擬交易所可以查交易規則與伺服器時間()
    {
        var exchange = CreateExchange();

        var symbols = await exchange.GetSymbolsAsync(CancellationToken.None);
        var symbol = await exchange.GetSymbolAsync("BTCUSDT", CancellationToken.None);
        var missing = await exchange.GetSymbolAsync("DOGEUSDT", CancellationToken.None);
        var time = await exchange.GetServerTimeAsync(CancellationToken.None);

        Assert.IsTrue(symbols.TryGetValue(out var list));
        Assert.HasCount(1, list);
        Assert.IsTrue(symbol.IsSuccess);
        Assert.AreEqual(TradeErrorCodes.SymbolNotFound, missing.Error!.Code);
        Assert.IsTrue(time.TryGetValue(out var serverTime));
        Assert.AreEqual(Start, serverTime);
    }

    [TestMethod]
    public async Task 送單之後持倉與帳戶快照都跟著變()
    {
        var exchange = CreateExchange();
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Side = OrderSide.Buy,
            OrderType = OrderType.Limit,
            Quantity = 0.0125m,
            Price = 50_000.16m,
            ClientOrderId = "pt-1",
        };

        var placed = await exchange.PlaceOrderAsync(request, CancellationToken.None);

        Assert.IsTrue(placed.TryGetValue(out var order), placed.Error?.ToString());
        Assert.AreEqual(OrderStatus.Filled, order.Status);
        Assert.AreEqual(0.012m, order.Quantity, "數量應被向下校正");
        Assert.AreEqual(50_000.2m, order.AverageFillPrice, "價格應被對齊");

        var position = await exchange.GetPositionAsync("BTCUSDT", CancellationToken.None);

        Assert.IsTrue(position.TryGetValue(out var held));
        Assert.AreEqual(0.012m, held.Quantity);
        Assert.IsTrue(held.IsLong);

        var snapshot = await exchange.GetAccountSnapshotAsync(CancellationToken.None);

        Assert.IsTrue(snapshot.TryGetValue(out var account));
        Assert.IsTrue(account.GetBalance("USDT").IsSuccess);
        Assert.IsTrue(account.GetPosition("BTCUSDT").IsSuccess);
    }

    [TestMethod]
    public async Task 用冪等識別碼查得回剛才那張單()
    {
        var exchange = CreateExchange();
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Side = OrderSide.Buy,
            OrderType = OrderType.Market,
            Quantity = 0.01m,
            ClientOrderId = "pt-idempotent",
        };

        _ = await exchange.PlaceOrderAsync(request, CancellationToken.None);

        var found = await exchange.GetOrderAsync(
            "BTCUSDT",
            OrderIdentifier.FromClientId("pt-idempotent"),
            CancellationToken.None);
        var missing = await exchange.GetOrderAsync(
            "BTCUSDT",
            OrderIdentifier.FromClientId("pt-unknown"),
            CancellationToken.None);

        Assert.IsTrue(found.IsSuccess);
        Assert.AreEqual(TradeErrorCodes.OrderNotFound, missing.Error!.Code);
        Assert.AreEqual(ErrorCategory.NotFound, missing.Error.Category);
    }

    [TestMethod]
    public async Task 不合格的委託在送出之前就被擋下()
    {
        var exchange = CreateExchange();
        var tooSmall = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Side = OrderSide.Buy,
            OrderType = OrderType.Limit,
            Quantity = 0.001m,
            Price = 50_000m,
        };

        var result = await exchange.PlaceOrderAsync(tooSmall, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.NotionalBelowMinimum, result.Error!.Code);
    }

    [TestMethod]
    public async Task 歷史K線可以查詢也擋得下不合法條件()
    {
        var exchange = CreateExchange();

        var klines = await exchange.GetKlinesAsync(
            new KlineQuery { Symbol = "BTCUSDT", Interval = KlineInterval.FifteenMinutes },
            CancellationToken.None);
        var invalid = await exchange.GetKlinesAsync(
            new KlineQuery { Symbol = "BTCUSDT", Interval = KlineInterval.FifteenMinutes, Limit = -1 },
            CancellationToken.None);

        Assert.IsTrue(klines.TryGetValue(out var candles));
        Assert.HasCount(5, candles);
        Assert.AreEqual(TradeErrorCodes.InvalidQuery, invalid.Error!.Code);
    }

    [TestMethod]
    public async Task 即時串流不需要網路也能列舉()
    {
        var exchange = CreateExchange();
        var received = new List<Kline>();

        await foreach (var item in exchange.SubscribeKlinesAsync(
            ["BTCUSDT"],
            KlineInterval.FifteenMinutes,
            CancellationToken.None))
        {
            Assert.IsTrue(item.TryGetValue(out var kline), item.Error?.ToString());
            received.Add(kline);
        }

        Assert.HasCount(5, received);
        Assert.IsTrue(received.TrueForAll(kline => kline.IsClosed));

        await foreach (var item in exchange.SubscribeMarkPricesAsync(
            ["BTCUSDT"],
            CancellationToken.None))
        {
            Assert.IsTrue(item.TryGetValue(out var mark));
            Assert.AreEqual(50_050m, mark.MarkPrice);
        }
    }

    [TestMethod]
    public async Task 成交與委託更新可以由私有串流推出()
    {
        var exchange = CreateExchange();
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Side = OrderSide.Sell,
            OrderType = OrderType.Market,
            Quantity = 0.01m,
        };

        _ = await exchange.PlaceOrderAsync(request, CancellationToken.None);

        var orders = 0;
        var fills = 0;

        await foreach (var item in exchange.SubscribeOrderUpdatesAsync(CancellationToken.None))
        {
            Assert.IsTrue(item.IsSuccess);
            orders++;
        }

        await foreach (var item in exchange.SubscribeTradeUpdatesAsync(CancellationToken.None))
        {
            Assert.IsTrue(item.TryGetValue(out var trade));
            Assert.AreEqual("USDT", trade.FeeAsMoney().Currency);
            fills++;
        }

        Assert.AreEqual(1, orders);
        Assert.AreEqual(1, fills);
    }

    [TestMethod]
    public async Task 其餘操作也都有明確結果()
    {
        var exchange = CreateExchange();

        Assert.IsTrue((await exchange.SetLeverageAsync("BTCUSDT", 5, CancellationToken.None)).IsSuccess);
        Assert.IsTrue((await exchange.SetMarginModeAsync("BTCUSDT", MarginMode.Isolated, CancellationToken.None)).IsSuccess);
        Assert.IsTrue((await exchange.CancelAllOrdersAsync("BTCUSDT", CancellationToken.None)).IsSuccess);
        Assert.IsTrue((await exchange.GetOpenOrdersAsync(cancellationToken: CancellationToken.None)).IsSuccess);
        Assert.IsTrue((await exchange.GetPositionsAsync(CancellationToken.None)).IsSuccess);

        var cancelled = await exchange.CancelOrderAsync(
            "BTCUSDT",
            OrderIdentifier.FromExchangeId("1"),
            CancellationToken.None);

        Assert.AreEqual(TradeErrorCodes.OrderNotFound, cancelled.Error!.Code);
    }

    [TestMethod]
    public async Task 帳戶變動與對帳訊號也由純記憶體實作滿足()
    {
        var exchange = CreateExchange();

        await exchange.PlaceOrderAsync(
            new OrderRequest
            {
                Symbol = "BTCUSDT",
                Side = OrderSide.Buy,
                OrderType = OrderType.Market,
                Quantity = 0.01m,
                ClientOrderId = "pt-uds",
            },
            CancellationToken.None);

        var updates = 0;

        await foreach (var item in exchange.SubscribeAccountUpdatesAsync(CancellationToken.None))
        {
            Assert.IsTrue(item.TryGetValue(out var update));

            // 增量:成交只動到一個部位,所以這一筆就只帶那一個,餘額清單是空的。
            Assert.AreEqual(AccountUpdateReason.Order, update.Reason);
            Assert.HasCount(1, update.Positions);
            Assert.IsEmpty(update.Balances);
            updates++;
        }

        Assert.AreEqual(1, updates);

        // 記憶體內的撮合不追繳保證金也不斷線。這兩條是空的,但「存在且可訂閱」本身就是要驗的事:
        // 回測要能走同一套介面,不能因為少了這兩個方法而編不過。
        await foreach (var _ in exchange.SubscribeMarginCallsAsync(CancellationToken.None))
        {
            Assert.Fail("純記憶體的撮合不應該產生保證金追繳。");
        }

        await foreach (var _ in exchange.SubscribeResyncSignalsAsync(CancellationToken.None))
        {
            Assert.Fail("純記憶體的撮合不會斷線,不應該要求對帳。");
        }
    }
}
