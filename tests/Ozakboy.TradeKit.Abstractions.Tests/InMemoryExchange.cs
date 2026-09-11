using System.Runtime.CompilerServices;

namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 純記憶體的模擬交易所,用來證明本套件的介面可以在完全沒有網路的情況下實作完成。
/// A purely in-memory exchange proving that every interface in this package can be implemented with no network at
/// all.
/// </summary>
/// <remarks>
/// 這不是可以拿去用的回測撮合器,只是驗收用的最小實作:所有委託立刻以委託價全部成交。
/// This is not a usable backtest engine, only the minimum implementation needed for the acceptance check: every
/// order fills completely and immediately at its own price.
/// </remarks>
internal sealed class InMemoryExchange : IExchangeClient, IMarketDataFeed, IUserDataFeed
{
    private readonly Dictionary<string, SymbolInfo> _symbols;
    private readonly Dictionary<string, Order> _orders = [];
    private readonly Dictionary<string, Position> _positions = [];
    private readonly List<Trade> _trades = [];
    private readonly List<Kline> _klines;
    private readonly DateTimeOffset _now;

    public InMemoryExchange(SymbolInfo symbol, IEnumerable<Kline> klines, DateTimeOffset now)
    {
        _symbols = new Dictionary<string, SymbolInfo>(StringComparer.Ordinal) { [symbol.Name] = symbol };
        _klines = [.. klines];
        _now = now;
    }

    public Task<Result<IReadOnlyList<SymbolInfo>>> GetSymbolsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success<IReadOnlyList<SymbolInfo>>([.. _symbols.Values]));

    public Task<Result<SymbolInfo>> GetSymbolAsync(string symbol, CancellationToken cancellationToken = default) =>
        Task.FromResult(_symbols.TryGetValue(symbol, out var info)
            ? Result.Success(info)
            : Result.Failure<SymbolInfo>(TradeErrors.SymbolNotFound(symbol)));

    public Task<Result<DateTimeOffset>> GetServerTimeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(_now));

    public Task<Result<AccountSnapshot>> GetAccountSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(new AccountSnapshot
        {
            Balances = [new Balance { Asset = "USDT", WalletBalance = 10_000m, AvailableBalance = 10_000m }],
            Positions = [.. _positions.Values],
            TakenAt = _now,
        }));

    public Task<Result<IReadOnlyList<Position>>> GetPositionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success<IReadOnlyList<Position>>([.. _positions.Values]));

    public Task<Result<Position>> GetPositionAsync(string symbol, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(
            _positions.TryGetValue(symbol, out var position) ? position : Position.Flat(symbol, _now)));

    public Task<Result<Order>> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_symbols.TryGetValue(request.Symbol, out var symbol))
        {
            return Task.FromResult(Result.Failure<Order>(TradeErrors.SymbolNotFound(request.Symbol)));
        }

        var normalization = request.NormalizeFor(symbol);

        if (!normalization.TryGetValue(out var normalized))
        {
            return Task.FromResult(Result.Failure<Order>(normalization.Error!));
        }

        var fillPrice = normalized.Price ?? normalized.StopPrice ?? _klines[^1].Close;
        var clientOrderId = normalized.ClientOrderId ?? $"sim-{_orders.Count + 1}";

        if (_orders.ContainsKey(clientOrderId))
        {
            return Task.FromResult(Result.Failure<Order>(
                new Error(TradeErrorCodes.DuplicateClientOrderId, "重複的用戶端訂單編號。Duplicate client order id.", ErrorCategory.Conflict)));
        }

        var order = new Order
        {
            Symbol = normalized.Symbol,
            ClientOrderId = clientOrderId,
            ExchangeOrderId = (_orders.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
            Side = normalized.Side,
            OrderType = normalized.OrderType,
            Status = OrderStatus.Filled,
            Quantity = normalized.Quantity,
            FilledQuantity = normalized.Quantity,
            AverageFillPrice = fillPrice,
            FilledNotional = fillPrice * normalized.Quantity,
            Price = normalized.Price,
            StopPrice = normalized.StopPrice,
            CreatedAt = _now,
            UpdatedAt = _now,
        };

        _orders[clientOrderId] = order;
        _trades.Add(new Trade
        {
            Symbol = order.Symbol,
            TradeId = order.ExchangeOrderId!,
            ClientOrderId = order.ClientOrderId,
            ExchangeOrderId = order.ExchangeOrderId,
            Side = order.Side,
            Price = fillPrice,
            Quantity = order.FilledQuantity,
            Fee = order.FilledNotional * 0.0004m,
            FeeAsset = "USDT",
            ExecutedAt = _now,
        });

        ApplyFill(order, fillPrice);

        return Task.FromResult(Result.Success(order));
    }

    public Task<Result<Order>> CancelOrderAsync(
        string symbol,
        OrderIdentifier identifier,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<Order>(TradeErrors.OrderNotFound(identifier)));

    public Task<Result> CancelAllOrdersAsync(string symbol, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());

    public Task<Result<Order>> GetOrderAsync(
        string symbol,
        OrderIdentifier identifier,
        CancellationToken cancellationToken = default)
    {
        foreach (var order in _orders.Values)
        {
            var matchesClient = identifier.ClientOrderId is { } clientId
                && string.Equals(order.ClientOrderId, clientId, StringComparison.Ordinal);
            var matchesExchange = identifier.ExchangeOrderId is { } exchangeId
                && string.Equals(order.ExchangeOrderId, exchangeId, StringComparison.Ordinal);

            if (matchesClient || matchesExchange)
            {
                return Task.FromResult(Result.Success(order));
            }
        }

        return Task.FromResult(Result.Failure<Order>(TradeErrors.OrderNotFound(identifier)));
    }

    public Task<Result<IReadOnlyList<Order>>> GetOpenOrdersAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success<IReadOnlyList<Order>>([]));

    public Task<Result> SetLeverageAsync(string symbol, int leverage, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());

    public Task<Result> SetMarginModeAsync(
        string symbol,
        MarginMode marginMode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());

    public Task<Result<IReadOnlyList<Kline>>> GetKlinesAsync(
        KlineQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var validation = query.Validate();

        return Task.FromResult(validation.IsFailure
            ? Result.Failure<IReadOnlyList<Kline>>(validation.Error!)
            : Result.Success<IReadOnlyList<Kline>>([.. _klines]));
    }

    public async IAsyncEnumerable<Result<Kline>> SubscribeKlinesAsync(
        IReadOnlyCollection<string> symbols,
        KlineInterval interval,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        foreach (var kline in _klines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask.ConfigureAwait(false);

            if (symbols.Contains(kline.Symbol) && kline.Interval == interval)
            {
                yield return kline;
            }
        }
    }

    public async IAsyncEnumerable<Result<MarkPriceUpdate>> SubscribeMarkPricesAsync(
        IReadOnlyCollection<string> symbols,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        foreach (var symbol in symbols)
        {
            await Task.CompletedTask.ConfigureAwait(false);

            yield return new MarkPriceUpdate
            {
                Symbol = symbol,
                MarkPrice = _klines[^1].Close,
                Timestamp = _now,
            };
        }
    }

    public async IAsyncEnumerable<Result<Order>> SubscribeOrderUpdatesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var order in _orders.Values)
        {
            await Task.CompletedTask.ConfigureAwait(false);

            yield return order;
        }
    }

    public async IAsyncEnumerable<Result<Trade>> SubscribeTradeUpdatesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var trade in _trades)
        {
            await Task.CompletedTask.ConfigureAwait(false);

            yield return trade;
        }
    }

    public async IAsyncEnumerable<Result<AccountUpdate>> SubscribeAccountUpdatesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var position in _positions.Values)
        {
            await Task.CompletedTask.ConfigureAwait(false);

            // 這個假交易所的部位只會因為成交而變,所以原因固定是 Order;送出的是單一部位的增量,
            // 正好示範 AccountUpdate 與 AccountSnapshot 的差別。
            yield return new AccountUpdate
            {
                Reason = AccountUpdateReason.Order,
                Positions =
                [
                    new PositionChange
                    {
                        Symbol = position.Symbol,
                        Quantity = position.Quantity,
                        Side = position.Side,
                        EntryPrice = position.EntryPrice,
                        UnrealizedPnl = position.UnrealizedPnl,
                        MarginMode = position.MarginMode,
                    },
                ],
                Timestamp = _now,
            };
        }
    }

    // 記憶體內的撮合不會追繳保證金,也不會斷線,所以這兩條永遠是空的 —— 回測的模擬實作同理。
    // An in-memory matcher never issues a margin call and never drops a connection, so both streams stay empty;
    // the same holds for a backtest implementation.
    public IAsyncEnumerable<Result<MarginCall>> SubscribeMarginCallsAsync(
        CancellationToken cancellationToken = default) =>
        AsyncEnumerable.Empty<Result<MarginCall>>();

    public IAsyncEnumerable<Result<ResyncRequired>> SubscribeResyncSignalsAsync(
        CancellationToken cancellationToken = default) =>
        AsyncEnumerable.Empty<Result<ResyncRequired>>();

    private void ApplyFill(Order order, decimal fillPrice)
    {
        var signed = order.Side == OrderSide.Buy ? order.FilledQuantity : -order.FilledQuantity;
        var existing = _positions.TryGetValue(order.Symbol, out var position)
            ? position
            : Position.Flat(order.Symbol, _now);

        _positions[order.Symbol] = existing with
        {
            Quantity = existing.Quantity + signed,
            EntryPrice = fillPrice,
            MarkPrice = fillPrice,
            UpdatedAt = _now,
        };
    }
}
