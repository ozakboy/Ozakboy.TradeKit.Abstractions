namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 委託在交易所端的狀態。
/// The lifecycle state of an order at the exchange.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// 未指定。屬於無效值,代表對映時漏掉了交易所回傳的狀態。
    /// Not specified. An invalid value meaning a mapping missed a status the exchange returned.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// 已被交易所接受,尚未有任何成交。
    /// Accepted by the exchange with nothing filled yet.
    /// </summary>
    New = 1,

    /// <summary>
    /// 部分成交,剩餘數量仍掛在簿上。
    /// Partially filled, with the remainder still resting on the book.
    /// </summary>
    PartiallyFilled = 2,

    /// <summary>
    /// 全部成交。
    /// Completely filled.
    /// </summary>
    Filled = 3,

    /// <summary>
    /// 已撤銷。
    /// Cancelled.
    /// </summary>
    Canceled = 4,

    /// <summary>
    /// 被交易所拒絕(參數不合法、風控攔截等)。
    /// Rejected by the exchange, for example on invalid parameters or a risk check.
    /// </summary>
    Rejected = 5,

    /// <summary>
    /// 因有效期限規則而失效(例如 IOC 未成交的部分)。
    /// Expired under the time-in-force rule, such as the unfilled remainder of an IOC order.
    /// </summary>
    Expired = 6,

    /// <summary>
    /// 撤單請求已送出,交易所尚未確認。
    /// A cancellation has been requested but not yet confirmed by the exchange.
    /// </summary>
    PendingCancel = 7,
}

/// <summary>
/// <see cref="OrderStatus"/> 的輔助方法。
/// Helper methods for <see cref="OrderStatus"/>.
/// </summary>
public static class OrderStatusExtensions
{
    /// <summary>
    /// 判斷委託是否仍可能再有成交(還活在簿上)。
    /// Determines whether the order can still receive fills, that is, whether it is still live.
    /// </summary>
    /// <param name="status">要判斷的狀態。The status to inspect.</param>
    /// <returns>
    /// 仍在簿上時回傳 <see langword="true"/>。
    /// <see langword="true"/> while the order is still on the book.
    /// </returns>
    /// <remarks>
    /// <see cref="OrderStatus.PendingCancel"/> 視為仍在簿上:撤單尚未被交易所確認之前,這張單隨時可能成交,
    /// 把它當成已結束會讓部位追蹤少算一筆。
    /// <see cref="OrderStatus.PendingCancel"/> counts as live: until the exchange confirms the cancellation the
    /// order can still fill, and treating it as finished loses a fill in position tracking.
    /// </remarks>
    public static bool IsOpen(this OrderStatus status) => status switch
    {
        OrderStatus.New or OrderStatus.PartiallyFilled or OrderStatus.PendingCancel => true,
        _ => false,
    };

    /// <summary>
    /// 判斷委託是否已經進入終態(不會再變化)。
    /// Determines whether the order has reached a terminal state and can no longer change.
    /// </summary>
    /// <param name="status">要判斷的狀態。The status to inspect.</param>
    /// <returns>
    /// 已是終態時回傳 <see langword="true"/>。<see cref="OrderStatus.Unspecified"/> 不算終態。
    /// <see langword="true"/> for a terminal state. <see cref="OrderStatus.Unspecified"/> is not terminal.
    /// </returns>
    public static bool IsFinal(this OrderStatus status) => status switch
    {
        OrderStatus.Filled or OrderStatus.Canceled or OrderStatus.Rejected or OrderStatus.Expired => true,
        _ => false,
    };
}
