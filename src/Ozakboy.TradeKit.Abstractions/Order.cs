namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 一張委託在交易所端的狀態快照。
/// A snapshot of one order as the exchange sees it.
/// </summary>
/// <remarks>
/// 所有時間欄位的語意都是 UTC。上層只在顯示給人看的時候才轉當地時區:交易系統裡混用時區,
/// 對帳與回測的時間軸就會錯開,而且錯開的量會隨夏令時間變動。
/// Every timestamp is UTC. Convert to local time only when displaying it: mixing time zones inside a trading
/// system skews the reconciliation and backtest timelines, and the skew changes with daylight saving.
/// </remarks>
public sealed record Order
{
    /// <summary>
    /// 交易對代碼。
    /// The symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 用戶端訂單編號(冪等識別碼)。
    /// The client order id, which acts as the idempotency key.
    /// </summary>
    public required string ClientOrderId { get; init; }

    /// <summary>
    /// 交易所產生的訂單編號。尚未取得時為 <see langword="null"/>。
    /// The order id assigned by the exchange, or <see langword="null"/> when it is not known yet.
    /// </summary>
    public string? ExchangeOrderId { get; init; }

    /// <summary>
    /// 買賣方向。
    /// The order side.
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// 委託類型。
    /// The order type.
    /// </summary>
    public required OrderType OrderType { get; init; }

    /// <summary>
    /// 委託狀態。
    /// The order status.
    /// </summary>
    public required OrderStatus Status { get; init; }

    /// <summary>
    /// 持倉方向,單向模式為 <see cref="PositionSide.Both"/>。
    /// The position side; <see cref="PositionSide.Both"/> in one-way mode.
    /// </summary>
    public PositionSide PositionSide { get; init; } = PositionSide.Both;

    /// <summary>
    /// 有效期限規則。
    /// The time in force.
    /// </summary>
    public TimeInForce TimeInForce { get; init; } = TimeInForce.GoodTilCanceled;

    /// <summary>
    /// 委託數量。<see cref="ClosePosition"/> 的委託在成交前可能為 0。
    /// The ordered quantity. It can be zero before a <see cref="ClosePosition"/> order fills.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// 已成交數量。
    /// The quantity filled so far.
    /// </summary>
    public decimal FilledQuantity { get; init; }

    /// <summary>
    /// 已成交部分的平均成交價。完全未成交時為 0。
    /// The average price of the filled portion, or zero when nothing has filled.
    /// </summary>
    public decimal AverageFillPrice { get; init; }

    /// <summary>
    /// 限價,市價單為 <see langword="null"/>。
    /// The limit price, or <see langword="null"/> for a market order.
    /// </summary>
    public decimal? Price { get; init; }

    /// <summary>
    /// 觸發價,非條件單為 <see langword="null"/>。
    /// The trigger price, or <see langword="null"/> for a non-conditional order.
    /// </summary>
    public decimal? StopPrice { get; init; }

    /// <summary>
    /// 是否為只減倉委託。
    /// Whether the order is reduce-only.
    /// </summary>
    public bool ReduceOnly { get; init; }

    /// <summary>
    /// 是否為全部平倉委託。
    /// Whether the order closes the whole position.
    /// </summary>
    public bool ClosePosition { get; init; }

    /// <summary>
    /// 累計成交金額(以計價幣計)。
    /// The cumulative filled notional, in quote asset units.
    /// </summary>
    public decimal FilledNotional { get; init; }

    /// <summary>
    /// 建立時間(UTC 語意)。
    /// The creation time, in UTC semantics.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 最後更新時間(UTC 語意)。
    /// The last update time, in UTC semantics.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// 尚未成交的數量。
    /// The quantity still unfilled.
    /// </summary>
    public decimal RemainingQuantity => Quantity - FilledQuantity;

    /// <summary>
    /// 是否仍在簿上(還可能再成交)。
    /// Whether the order is still live and can receive more fills.
    /// </summary>
    public bool IsOpen => Status.IsOpen();

    /// <summary>
    /// 是否已進入終態。
    /// Whether the order has reached a terminal state.
    /// </summary>
    public bool IsFinal => Status.IsFinal();

    /// <summary>
    /// 取得這張委託的識別碼:有交易所編號時用交易所編號,否則用用戶端編號。
    /// Returns the identifier for this order, preferring the exchange id and falling back to the client id.
    /// </summary>
    /// <returns>識別碼。The identifier.</returns>
    public OrderIdentifier GetIdentifier() =>
        string.IsNullOrWhiteSpace(ExchangeOrderId)
            ? OrderIdentifier.FromClientId(ClientOrderId)
            : OrderIdentifier.FromExchangeId(ExchangeOrderId);
}
