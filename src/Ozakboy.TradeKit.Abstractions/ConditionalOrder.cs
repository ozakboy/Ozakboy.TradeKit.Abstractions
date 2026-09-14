namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 一張條件單在交易所端的狀態快照。
/// A snapshot of one conditional order as the exchange sees it.
/// </summary>
/// <remarks>
/// <para>
/// 刻意不帶已成交數量與均價這類欄位:條件單在觸發之前沒有成交可言,觸發之後成交發生在
/// <see cref="TriggeredOrderId"/> 指向的那張委託上,那些數字該從 <see cref="Order"/> 讀。
/// 在這裡放一組恆為 0 的成交欄位,只會讓「還沒觸發」與「觸發了但沒成交」看起來一模一樣。
/// Fill quantity and average price are deliberately absent: before the trigger there is nothing to fill, and
/// after it the fills belong to the order named by <see cref="TriggeredOrderId"/>, where those numbers should be
/// read from. Carrying a set of permanently zero fill fields here would only make "not triggered yet" and
/// "triggered but unfilled" look identical.
/// </para>
/// <para>
/// 所有時間欄位的語意都是 UTC,理由同 <see cref="Order"/>。
/// Every timestamp is UTC, for the reasons given on <see cref="Order"/>.
/// </para>
/// </remarks>
public sealed record ConditionalOrder
{
    /// <summary>
    /// 交易對代碼。
    /// The symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 用戶端條件單編號(冪等識別碼)。
    /// The client conditional order id, which acts as the idempotency key.
    /// </summary>
    public required string ClientConditionalOrderId { get; init; }

    /// <summary>
    /// 交易所產生的條件單編號。尚未取得時為 <see langword="null"/>。
    /// The conditional order id assigned by the exchange, or <see langword="null"/> when it is not known yet.
    /// </summary>
    public string? ExchangeConditionalOrderId { get; init; }

    /// <summary>
    /// 買賣方向。
    /// The order side.
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// 條件單類型。
    /// The conditional order type.
    /// </summary>
    public required ConditionalOrderType ConditionalOrderType { get; init; }

    /// <summary>
    /// 條件單狀態。
    /// The conditional order status.
    /// </summary>
    public required ConditionalOrderStatus Status { get; init; }

    /// <summary>
    /// 持倉方向,單向模式為 <see cref="PositionSide.Both"/>。
    /// The position side; <see cref="PositionSide.Both"/> in one-way mode.
    /// </summary>
    public PositionSide PositionSide { get; init; } = PositionSide.Both;

    /// <summary>
    /// 委託數量。<see cref="ClosePosition"/> 的條件單為 0。
    /// The ordered quantity. Zero for a <see cref="ClosePosition"/> conditional order.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// 觸發價。移動停損在啟動之前可能為 <see langword="null"/>。
    /// The trigger price, which can be <see langword="null"/> for a trailing stop that has not activated.
    /// </summary>
    public decimal? TriggerPrice { get; init; }

    /// <summary>
    /// 觸發價與哪一種價格比較。
    /// Which price the trigger price is compared against.
    /// </summary>
    public TriggerPriceType TriggerPriceType { get; init; } = TriggerPriceType.MarkPrice;

    /// <summary>
    /// 觸發後掛出的限價,市價類型為 <see langword="null"/>。
    /// The limit price used after the trigger, or <see langword="null"/> for the market-style types.
    /// </summary>
    public decimal? Price { get; init; }

    /// <summary>
    /// 觸發後掛出的委託的有效期限規則。
    /// The time in force of the order placed after the trigger.
    /// </summary>
    public TimeInForce TimeInForce { get; init; } = TimeInForce.GoodTilCanceled;

    /// <summary>
    /// 移動停損的回撤比例(百分比),其他類型為 <see langword="null"/>。
    /// The trailing stop callback rate as a percentage, or <see langword="null"/> for the other types.
    /// </summary>
    public decimal? CallbackRate { get; init; }

    /// <summary>
    /// 移動停損的啟動價,其他類型為 <see langword="null"/>。
    /// The trailing stop activation price, or <see langword="null"/> for the other types.
    /// </summary>
    public decimal? ActivationPrice { get; init; }

    /// <summary>
    /// 是否為只減倉條件單。
    /// Whether the conditional order is reduce-only.
    /// </summary>
    public bool ReduceOnly { get; init; }

    /// <summary>
    /// 是否為全部平倉條件單。
    /// Whether the conditional order closes the whole position.
    /// </summary>
    public bool ClosePosition { get; init; }

    /// <summary>
    /// 觸發之後交易所實際送出的那張委託的編號。尚未觸發時為 <see langword="null"/>。
    /// The id of the order the exchange actually submitted once this triggered, or <see langword="null"/> while it
    /// has not triggered.
    /// </summary>
    /// <remarks>
    /// 這是把條件單接回一般委託與成交的唯一線索:成交、手續費、實際成交價都掛在這個編號底下,
    /// 條件單本身查不到。
    /// This is the only link back to ordinary orders and fills: the executions, fees, and actual fill price all
    /// hang off this id and cannot be read from the conditional order itself.
    /// </remarks>
    public string? TriggeredOrderId { get; init; }

    /// <summary>
    /// 觸發時間(UTC 語意)。尚未觸發時為 <see langword="null"/>。
    /// The time it triggered, in UTC semantics, or <see langword="null"/> while it has not triggered.
    /// </summary>
    public DateTimeOffset? TriggeredAt { get; init; }

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
    /// 是否仍然有效(還在等觸發,或觸發後的委託還在簿上)。
    /// Whether the conditional order is still live, either waiting to trigger or with its resulting order still on
    /// the book.
    /// </summary>
    public bool IsOpen => Status.IsOpen();

    /// <summary>
    /// 是否已進入終態。
    /// Whether the conditional order has reached a terminal state.
    /// </summary>
    public bool IsFinal => Status.IsFinal();

    /// <summary>
    /// 取得這張條件單的識別碼:有交易所編號時用交易所編號,否則用用戶端編號。
    /// Returns the identifier for this conditional order, preferring the exchange id and falling back to the
    /// client id.
    /// </summary>
    /// <returns>識別碼。The identifier.</returns>
    public ConditionalOrderIdentifier GetIdentifier() =>
        string.IsNullOrWhiteSpace(ExchangeConditionalOrderId)
            ? ConditionalOrderIdentifier.FromClientId(ClientConditionalOrderId)
            : ConditionalOrderIdentifier.FromExchangeId(ExchangeConditionalOrderId);
}
