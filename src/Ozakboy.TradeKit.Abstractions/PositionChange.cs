namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 帳戶變動事件裡的一筆部位增量:只含事件真的帶來的欄位。
/// One position entry inside an account change event, carrying only what the event actually delivers.
/// </summary>
/// <remarks>
/// <para>
/// <b>刻意沒有標記價、名目價值、槓桿與強平價。</b>交易所的帳戶變動事件不帶這些值。先前這裡用的是完整的
/// <see cref="Position"/>,拿不到的欄位只能填 0 —— 其中 <see cref="Position.Notional"/> 等於
/// 「數量 × 標記價」,於是恆為 0,風控讀到的就是「這個部位沒有曝險」。欄位不存在,編譯器才擋得住那種誤讀。
/// <b>There is deliberately no mark price, notional, leverage, or liquidation price.</b> Exchanges' account change
/// events do not carry them. A full <see cref="Position"/> used to stand here with the missing fields zeroed —
/// and <see cref="Position.Notional"/>, being quantity times mark price, was therefore always zero, which a risk
/// check reads as "this position carries no exposure". Only an absent member lets the compiler stop that reading.
/// </para>
/// <para>
/// 要評估曝險,請用 <see cref="IExchangeClient"/> 查持倉,或以標記價串流乘上這裡的數量。
/// For exposure, query positions through <see cref="IExchangeClient"/>, or multiply this quantity by a mark price
/// from the mark price stream.
/// </para>
/// </remarks>
public sealed record PositionChange
{
    /// <summary>
    /// 交易對代碼。
    /// The symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 變動後的持倉數量,帶正負號:多單為正、空單為負、已平倉為 0。
    /// The position size after the change, signed: positive long, negative short, zero when closed.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// 持倉方向。單向持倉模式下為 <see cref="PositionSide.Both"/>。
    /// The position side; <see cref="PositionSide.Both"/> in one-way mode.
    /// </summary>
    public PositionSide Side { get; init; } = PositionSide.Both;

    /// <summary>
    /// 開倉均價。已平倉時為 0。
    /// The average entry price; zero once the position is closed.
    /// </summary>
    public decimal EntryPrice { get; init; }

    /// <summary>
    /// 交易所計算的未實現損益。
    /// The unrealised PnL as computed by the exchange.
    /// </summary>
    public decimal UnrealizedPnl { get; init; }

    /// <summary>
    /// 這個部位的累計已實現損益。交易所未提供時為 <see langword="null"/>。
    /// The position's accumulated realised PnL, or <see langword="null"/> when the exchange does not provide it.
    /// </summary>
    public decimal? AccumulatedRealizedPnl { get; init; }

    /// <summary>
    /// 保證金模式。
    /// The margin mode.
    /// </summary>
    public MarginMode MarginMode { get; init; } = MarginMode.Cross;

    /// <summary>
    /// 逐倉保證金。全倉部位或交易所未提供時為 <see langword="null"/>。
    /// The isolated margin, or <see langword="null"/> for a cross position or when not provided.
    /// </summary>
    public decimal? IsolatedMargin { get; init; }

    /// <summary>
    /// 是否已平倉。
    /// Whether the position is closed.
    /// </summary>
    public bool IsFlat => Quantity == 0m;
}
