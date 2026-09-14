namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 條件單在交易所端的狀態。
/// The lifecycle state of a conditional order at the exchange.
/// </summary>
/// <remarks>
/// <para>
/// 與 <see cref="OrderStatus"/> 分開,是因為條件單的生命週期多了一段:它在「等待觸發」期間根本還不是
/// 一張簿上的委託,沒有成交數量可言;觸發之後才生出一張真正的委託,那張委託的狀態才由
/// <see cref="OrderStatus"/> 描述。把兩者混用會讓「等待觸發」被誤讀成「已掛單但還沒成交」,
/// 而這兩件事對風控的意義完全不同 —— 前者對簿上的深度與保證金都還沒有任何影響。
/// It is separate from <see cref="OrderStatus"/> because a conditional order has an extra stage: while it waits
/// for its trigger it is not an order on the book at all and has no fill quantity. Only after triggering does a
/// real order come into existence, and that order's state is what <see cref="OrderStatus"/> describes. Conflating
/// the two makes "waiting to trigger" read as "resting but unfilled", and to a risk check those mean entirely
/// different things — the former has yet to touch either book depth or margin.
/// </para>
/// <para>
/// 零值同樣保留為 <see cref="Unspecified"/>:交易所新增一個狀態字串而對映沒跟上時,結果必須是一個
/// 明顯無效的值,不可以落在任何看起來合理的狀態上。
/// The zero value is again <see cref="Unspecified"/>: when the exchange adds a status string that the mapping has
/// not caught up with, the result must be an obviously invalid value rather than any plausible-looking state.
/// </para>
/// </remarks>
public enum ConditionalOrderStatus
{
    /// <summary>
    /// 未指定。屬於無效值,代表對映時漏掉了交易所回傳的狀態。
    /// Not specified. An invalid value meaning a mapping missed a status the exchange returned.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// 已被交易所接受,正在等待觸發價到達。尚未在簿上,也還沒有對應的委託。
    /// Accepted by the exchange and waiting for its trigger price. Not on the book, and with no order behind it
    /// yet.
    /// </summary>
    New = 1,

    /// <summary>
    /// 觸發價已到達,交易所已送出對應的委託。
    /// <see cref="ConditionalOrder.TriggeredOrderId"/> 從這一刻起才會有值。
    /// The trigger price was reached and the exchange has submitted the resulting order.
    /// <see cref="ConditionalOrder.TriggeredOrderId"/> only carries a value from this point on.
    /// </summary>
    Triggered = 2,

    /// <summary>
    /// 觸發後產生的委託已全部成交。
    /// The order created by the trigger has filled completely.
    /// </summary>
    Filled = 3,

    /// <summary>
    /// 已撤銷(呼叫端主動撤,或交易所因部位歸零而清掉)。
    /// Cancelled, either by the caller or by the exchange once the position went flat.
    /// </summary>
    Canceled = 4,

    /// <summary>
    /// 已失效(例如觸發後產生的委託因有效期限規則被取消)。
    /// Expired, for example when the order created by the trigger was cancelled under its time-in-force rule.
    /// </summary>
    Expired = 5,

    /// <summary>
    /// 被交易所拒絕。
    /// Rejected by the exchange.
    /// </summary>
    Rejected = 6,
}

/// <summary>
/// <see cref="ConditionalOrderStatus"/> 的輔助方法。
/// Helper methods for <see cref="ConditionalOrderStatus"/>.
/// </summary>
public static class ConditionalOrderStatusExtensions
{
    /// <summary>
    /// 判斷條件單是否仍然有效(還會等待觸發,或觸發後的委託還在簿上)。
    /// Determines whether the conditional order is still live, meaning it is still waiting to trigger or the
    /// order it created is still on the book.
    /// </summary>
    /// <param name="status">要判斷的狀態。The status to inspect.</param>
    /// <returns>
    /// 仍然有效時回傳 <see langword="true"/>。
    /// <see langword="true"/> while the conditional order is still live.
    /// </returns>
    /// <remarks>
    /// <see cref="ConditionalOrderStatus.Triggered"/> 算有效:那張觸發出來的委託還沒成交完,
    /// 把它當成已結束,緊急出場流程就會漏撤這一張。
    /// <see cref="ConditionalOrderStatus.Triggered"/> counts as live: the order it produced has not finished
    /// filling, and treating it as done leaves that order uncancelled on an emergency exit.
    /// </remarks>
    public static bool IsOpen(this ConditionalOrderStatus status) => status switch
    {
        ConditionalOrderStatus.New or ConditionalOrderStatus.Triggered => true,
        _ => false,
    };

    /// <summary>
    /// 判斷條件單是否已經進入終態(不會再變化)。
    /// Determines whether the conditional order has reached a terminal state and can no longer change.
    /// </summary>
    /// <param name="status">要判斷的狀態。The status to inspect.</param>
    /// <returns>
    /// 已是終態時回傳 <see langword="true"/>。<see cref="ConditionalOrderStatus.Unspecified"/> 不算終態。
    /// <see langword="true"/> for a terminal state. <see cref="ConditionalOrderStatus.Unspecified"/> is not
    /// terminal.
    /// </returns>
    public static bool IsFinal(this ConditionalOrderStatus status) => status switch
    {
        ConditionalOrderStatus.Filled
            or ConditionalOrderStatus.Canceled
            or ConditionalOrderStatus.Expired
            or ConditionalOrderStatus.Rejected => true,
        _ => false,
    };
}
