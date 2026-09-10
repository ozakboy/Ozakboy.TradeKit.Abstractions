namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 一個商品目前的持倉。
/// The current position on one symbol.
/// </summary>
/// <remarks>
/// <see cref="Quantity"/> 帶正負號:正數是多單、負數是空單、零是空手。用正負號而不是另一個方向欄位,
/// 是因為淨部位的加總、平倉數量的計算都直接是算術運算,不必到處寫 <c>if (side == Short) q = -q;</c>,
/// 那種寫法只要漏一處就會算出反向的部位。
/// <see cref="Quantity"/> is signed: positive is long, negative is short, zero is flat. A sign rather than a
/// separate direction field keeps netting and close-out arithmetic plain, with no <c>if (side == Short) q = -q;</c>
/// scattered around — miss one of those and the computed position is inverted.
/// </remarks>
public sealed record Position
{
    /// <summary>
    /// 交易對代碼。
    /// The symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 帶正負號的持倉數量:正數為多單,負數為空單,零為空手。
    /// The signed position size: positive for long, negative for short, zero for flat.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// 交易所的持倉方向欄位。單向模式一律為 <see cref="PositionSide.Both"/>,方向請看
    /// <see cref="Quantity"/> 的正負號。
    /// The exchange's position side field. It is always <see cref="PositionSide.Both"/> in one-way mode, where the
    /// direction comes from the sign of <see cref="Quantity"/>.
    /// </summary>
    public PositionSide Side { get; init; } = PositionSide.Both;

    /// <summary>
    /// 開倉均價。空手時為 0。
    /// The average entry price, or zero when flat.
    /// </summary>
    public decimal EntryPrice { get; init; }

    /// <summary>
    /// 目前的標記價。
    /// The current mark price.
    /// </summary>
    public decimal MarkPrice { get; init; }

    /// <summary>
    /// 未實現損益,以計價幣計。
    /// The unrealised profit and loss, in quote asset units.
    /// </summary>
    public decimal UnrealizedPnl { get; init; }

    /// <summary>
    /// 強制平倉價。無持倉或交易所未提供時為 <see langword="null"/>。
    /// The liquidation price, or <see langword="null"/> when flat or not provided by the exchange.
    /// </summary>
    public decimal? LiquidationPrice { get; init; }

    /// <summary>
    /// 槓桿倍數。
    /// The leverage multiplier.
    /// </summary>
    public int Leverage { get; init; } = 1;

    /// <summary>
    /// 保證金模式。
    /// The margin mode.
    /// </summary>
    public MarginMode MarginMode { get; init; } = MarginMode.Cross;

    /// <summary>
    /// 這個部位佔用的保證金,以計價幣計。
    /// The margin allocated to this position, in quote asset units.
    /// </summary>
    public decimal Margin { get; init; }

    /// <summary>
    /// 快照時間(UTC 語意)。
    /// The time of this snapshot, in UTC semantics.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// 是否為多單。
    /// Whether the position is long.
    /// </summary>
    public bool IsLong => Quantity > 0m;

    /// <summary>
    /// 是否為空單。
    /// Whether the position is short.
    /// </summary>
    public bool IsShort => Quantity < 0m;

    /// <summary>
    /// 是否空手。
    /// Whether the position is flat.
    /// </summary>
    public bool IsFlat => Quantity == 0m;

    /// <summary>
    /// 不帶正負號的持倉數量。
    /// The absolute position size.
    /// </summary>
    public decimal AbsoluteQuantity => Math.Abs(Quantity);

    /// <summary>
    /// 以標記價計算的名目價值。
    /// The notional value at the mark price.
    /// </summary>
    public decimal Notional => AbsoluteQuantity * MarkPrice;

    /// <summary>
    /// 平掉這個部位需要的委託方向。空手時為 <see cref="OrderSide.Unspecified"/>。
    /// The order side needed to close this position, or <see cref="OrderSide.Unspecified"/> when flat.
    /// </summary>
    public OrderSide ClosingSide => Quantity switch
    {
        > 0m => OrderSide.Sell,
        < 0m => OrderSide.Buy,
        _ => OrderSide.Unspecified,
    };

    /// <summary>
    /// 建立一個空手的持倉,供「查無持倉」時回傳,避免呼叫端到處判斷 <see langword="null"/>。
    /// Creates a flat position, so a "no position" lookup can return a value instead of forcing
    /// <see langword="null"/> checks on the caller.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <param name="asOf">快照時間(UTC 語意)。The snapshot time, in UTC semantics.</param>
    /// <returns>數量為零的持倉。A position with zero quantity.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="symbol"/> 為空白時擲出。
    /// Thrown when <paramref name="symbol"/> is blank.
    /// </exception>
    public static Position Flat(string symbol, DateTimeOffset asOf)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        return new Position { Symbol = symbol, UpdatedAt = asOf };
    }
}
