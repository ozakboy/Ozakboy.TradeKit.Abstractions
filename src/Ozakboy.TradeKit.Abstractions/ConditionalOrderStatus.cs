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
    /// 觸發價已到達,委託已送往撮合引擎,但還沒被接受。
    /// The trigger price was reached and the order has been forwarded to the matching engine, but not yet
    /// accepted by it.
    /// </summary>
    /// <remarks>
    /// 與 <see cref="Triggered"/> 分開,是因為這一段有可能以 <see cref="Rejected"/> 收場 ——
    /// 條件單在觸發<b>之前</b>通常不做保證金檢查,檢查發生在這一刻。把兩者併成一個狀態,
    /// 就看不出「已經確定掛上去了」與「還可能被打回來」的差別,而那正是停損最危險的幾百毫秒。
    /// It is separate from <see cref="Triggered"/> because this stage can still end in
    /// <see cref="Rejected"/>: a conditional order is typically not margin-checked <b>before</b> it triggers,
    /// and the check happens here. Merging the two hides the difference between "it is definitely on the book"
    /// and "it may yet bounce" — which are the most dangerous few hundred milliseconds a stop has.
    /// </remarks>
    Triggering = 2,

    /// <summary>
    /// 觸發價已到達,對應的委託已經進入撮合引擎。
    /// <see cref="ConditionalOrder.TriggeredOrderId"/> 從這一刻起才會有值。
    /// The trigger price was reached and the resulting order is in the matching engine.
    /// <see cref="ConditionalOrder.TriggeredOrderId"/> only carries a value from this point on.
    /// </summary>
    Triggered = 3,

    /// <summary>
    /// 觸發後產生的委託已全部成交。
    /// The order created by the trigger has filled completely.
    /// </summary>
    /// <remarks>
    /// 只有在交易所<b>明說</b>是成交時才用這個值。交易所若只說「那張委託結束了」而沒說是成交還是被撤,
    /// 正確的值是 <see cref="Finished"/>。
    /// Use this only when the exchange <b>says</b> it filled. When it reports merely that the resulting order
    /// ended, without distinguishing a fill from a cancellation, the correct value is <see cref="Finished"/>.
    /// </remarks>
    Filled = 4,

    /// <summary>
    /// 觸發後產生的委託已經結束,但交易所沒說是成交還是被撤。
    /// The order created by the trigger has ended, without the exchange saying whether it filled or was
    /// cancelled.
    /// </summary>
    /// <remarks>
    /// 這是個誠實的「不知道是哪一種」,不是 <see cref="Filled"/> 的同義詞。有些交易所對條件單只回報到
    /// 「那張委託結束了」為止,要知道究竟成交了多少,必須拿
    /// <see cref="ConditionalOrder.TriggeredOrderId"/> 去查那張委託。把它當成成交,
    /// 會讓一張觸發後被撤掉的停損在帳上變成一次不存在的平倉。
    /// This is an honest "which of the two is unknown" rather than a synonym for <see cref="Filled"/>. Some
    /// exchanges report a conditional order only as far as "the resulting order is over", and finding out how
    /// much actually filled means looking that order up by
    /// <see cref="ConditionalOrder.TriggeredOrderId"/>. Reading it as a fill turns a stop that was cancelled
    /// after triggering into a close that never happened.
    /// </remarks>
    Finished = 5,

    /// <summary>
    /// 已撤銷(呼叫端主動撤,或交易所因部位歸零而清掉)。
    /// Cancelled, either by the caller or by the exchange once the position went flat.
    /// </summary>
    Canceled = 6,

    /// <summary>
    /// 已失效:交易所把這張還沒觸發的條件單清掉了(常見於部位已經平光,停損失去對象)。
    /// Expired: the exchange removed a conditional order that had not triggered, typically because the position
    /// it guarded is gone and the stop has nothing left to protect.
    /// </summary>
    Expired = 7,

    /// <summary>
    /// 被交易所拒絕。觸發之後才被打回來也算這一種。
    /// Rejected by the exchange, including a rejection that arrives after the trigger.
    /// </summary>
    /// <remarks>
    /// 這是條件單最危險的終態:上層以為部位有停損護著,實際上那張單從來沒有進到簿上。
    /// 拒絕原因通常只在串流事件裡出現一次,見 <see cref="ConditionalOrderUpdate.RejectReason"/>。
    /// This is the dangerous terminal state: the caller believes the position is protected while the order
    /// never reached the book. The reason usually appears exactly once, on the stream event — see
    /// <see cref="ConditionalOrderUpdate.RejectReason"/>.
    /// </remarks>
    Rejected = 8,
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
    /// <see cref="ConditionalOrderStatus.Triggering"/> 與 <see cref="ConditionalOrderStatus.Triggered"/>
    /// 都算有效:那張觸發出來的委託還沒成交完,把它當成已結束,緊急出場流程就會漏撤這一張。
    /// Both <see cref="ConditionalOrderStatus.Triggering"/> and
    /// <see cref="ConditionalOrderStatus.Triggered"/> count as live: the order they produced has not finished
    /// filling, and treating them as done leaves that order uncancelled on an emergency exit.
    /// </remarks>
    public static bool IsOpen(this ConditionalOrderStatus status) => status switch
    {
        ConditionalOrderStatus.New
            or ConditionalOrderStatus.Triggering
            or ConditionalOrderStatus.Triggered => true,
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
            or ConditionalOrderStatus.Finished
            or ConditionalOrderStatus.Canceled
            or ConditionalOrderStatus.Expired
            or ConditionalOrderStatus.Rejected => true,
        _ => false,
    };
}
