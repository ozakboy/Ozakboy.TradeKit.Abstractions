namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 保證金追繳警告裡的一個部位:只含事件真的帶來的欄位。
/// One position inside a margin call, carrying only what the event actually delivers.
/// </summary>
/// <remarks>
/// 追繳事件帶標記價與維持保證金,但不帶開倉均價與槓桿。先前用完整的 <see cref="Position"/> 時,
/// 開倉均價只能填 0、槓桿停在預設的 1 —— 兩個看起來合理、實際上是假的數字。這裡只放拿得到的,
/// 而因為標記價是真的,<see cref="Notional"/> 也是真的。
/// A margin call carries the mark price and maintenance margin but not the entry price or leverage. With a full
/// <see cref="Position"/> the entry price had to be zero and leverage sat at its default of one — two plausible
/// numbers that were false. Only what arrives is kept here, and because the mark price is real, so is
/// <see cref="Notional"/>.
/// </remarks>
public sealed record MarginCallPosition
{
    /// <summary>
    /// 交易對代碼。
    /// The symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 持倉數量,帶正負號:多單為正、空單為負。
    /// The position size, signed: positive long, negative short.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// 持倉方向。單向持倉模式下為 <see cref="PositionSide.Both"/>。
    /// The position side; <see cref="PositionSide.Both"/> in one-way mode.
    /// </summary>
    public PositionSide Side { get; init; } = PositionSide.Both;

    /// <summary>
    /// 標記價。
    /// The mark price.
    /// </summary>
    public required decimal MarkPrice { get; init; }

    /// <summary>
    /// 交易所計算的未實現損益。
    /// The unrealised PnL as computed by the exchange.
    /// </summary>
    public decimal UnrealizedPnl { get; init; }

    /// <summary>
    /// 維持保證金。帳戶權益跌破它就會被強平。交易所未提供時為 <see langword="null"/>。
    /// The maintenance margin; equity below it is liquidated. <see langword="null"/> when not provided.
    /// </summary>
    public decimal? MaintenanceMargin { get; init; }

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
    /// 名目價值:數量絕對值乘以標記價。
    /// The notional value: absolute quantity times the mark price.
    /// </summary>
    public decimal Notional => Math.Abs(Quantity) * MarkPrice;
}
