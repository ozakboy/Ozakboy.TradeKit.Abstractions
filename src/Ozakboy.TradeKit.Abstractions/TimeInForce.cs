namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 限價單的有效期限規則。
/// How long a limit order stays in force.
/// </summary>
/// <remarks>
/// 只有限價類型的委託需要這個欄位;市價與停損市價單會忽略它。
/// Only limit-style orders use this field; market and stop-market orders ignore it.
/// </remarks>
public enum TimeInForce
{
    /// <summary>
    /// 未指定。屬於無效值,限價單送出前必須被驗證擋下。
    /// Not specified. An invalid value that validation must reject for limit orders.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// GTC:掛單直到成交或被撤銷。
    /// Good til canceled: the order rests until it fills or is cancelled.
    /// </summary>
    GoodTilCanceled = 1,

    /// <summary>
    /// IOC:立即成交可成交的部分,其餘立刻取消。
    /// Immediate or cancel: fill what can be filled right now, cancel the remainder.
    /// </summary>
    ImmediateOrCancel = 2,

    /// <summary>
    /// FOK:必須全部立即成交,否則整張取消。
    /// Fill or kill: the whole quantity must fill immediately, otherwise the order is cancelled.
    /// </summary>
    FillOrKill = 3,

    /// <summary>
    /// GTX(只做 Maker):若這張單會立刻與對手單成交就直接取消,確保拿到掛單方手續費。
    /// Good til crossing, also known as post-only: the order is cancelled if it would match immediately, which
    /// guarantees the maker fee.
    /// </summary>
    GoodTilCrossing = 4,
}
