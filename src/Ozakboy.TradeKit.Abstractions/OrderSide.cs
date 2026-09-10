namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 委託的買賣方向。
/// The side of an order.
/// </summary>
/// <remarks>
/// 零值刻意保留為 <see cref="Unspecified"/>。合約下單的方向填錯就是反向部位,若讓 <see langword="default"/>
/// 落在 <see cref="Buy"/>,任何忘記賦值的欄位都會靜默變成買單送出交易所。
/// The zero value is deliberately <see cref="Unspecified"/>. Getting the side wrong on a derivatives order means
/// an inverted position; if <see langword="default"/> mapped to <see cref="Buy"/>, any field left unassigned would
/// silently reach the exchange as a buy order.
/// </remarks>
public enum OrderSide
{
    /// <summary>
    /// 未指定。屬於無效值,送單前必須被驗證擋下。
    /// Not specified. An invalid value that order validation must reject.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// 買進(做多或平空)。
    /// Buy, which opens a long position or closes a short one.
    /// </summary>
    Buy = 1,

    /// <summary>
    /// 賣出(做空或平多)。
    /// Sell, which opens a short position or closes a long one.
    /// </summary>
    Sell = 2,
}
