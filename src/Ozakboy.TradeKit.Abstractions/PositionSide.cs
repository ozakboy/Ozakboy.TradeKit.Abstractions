namespace Ozakboy.TradeKit.Abstractions;

// CA1720:Long 與 Short 是型別名稱(Int64、Int16)的別名,分析器建議改名。
// 這裡刻意保留 —— 多空方向在交易領域就叫 Long / Short,所有交易所 API、所有交易員、所有文件都是這個字。
// 改成 LongPosition / ShortPosition 只會讓每一處 switch 讀起來更差,而且與外部文件對不起來。
#pragma warning disable CA1720

/// <summary>
/// 持倉方向。對應交易所的「持倉模式」設定:單向模式只用 <see cref="Both"/>,雙向(避險)模式才會出現
/// <see cref="Long"/> 與 <see cref="Short"/>。
/// The position side, mirroring the exchange's position mode: one-way mode uses only <see cref="Both"/>, while
/// hedge mode splits into <see cref="Long"/> and <see cref="Short"/> buckets.
/// </summary>
/// <remarks>
/// 本專案以單向模式為主,因此 <see cref="Both"/> 為零值也是預設值;列舉仍保留雙向模式的兩個值,
/// 讓實作套件不必為了避險模式再開一組型別。
/// This project runs in one-way mode, so <see cref="Both"/> is both the zero value and the default. The two hedge
/// mode values are still present so that an implementation does not need a parallel type to support it.
/// </remarks>
public enum PositionSide
{
    /// <summary>
    /// 單向模式的唯一持倉方向:同一商品只有一個淨部位,方向由數量正負決定。
    /// The single bucket used in one-way mode: one net position per symbol, with direction carried by the sign of
    /// the quantity.
    /// </summary>
    Both = 0,

    /// <summary>
    /// 雙向模式的多單持倉。
    /// The long bucket in hedge mode.
    /// </summary>
    Long = 1,

    /// <summary>
    /// 雙向模式的空單持倉。
    /// The short bucket in hedge mode.
    /// </summary>
    Short = 2,
}

#pragma warning restore CA1720
