namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 委託被交易所以「到期 / 取消」結束的原因。
/// Why the exchange ended an order as expired or cancelled.
/// </summary>
/// <remarks>
/// <para>
/// 同樣是 <see cref="OrderStatus.Expired"/>,背後的原因可以完全不同:<c>IOC</c> 沒成交的部分被取消是正常結果,
/// 強平期間被清掉、商品下架、被另一張只減倉單擠掉,則都是需要上層另外處理的事件。只看狀態分不出來。
/// The same <see cref="OrderStatus.Expired"/> can hide entirely different causes: the unfilled remainder of an
/// <c>IOC</c> being cancelled is an ordinary outcome, while being cleared during a liquidation, a delisting, or being
/// knocked out by a reduce-only order are events a caller needs to handle on their own. The status alone cannot tell
/// them apart.
/// </para>
/// <para>
/// 交易所沒有給原因時,<see cref="Order.ExpiryReason"/> 是 <see langword="null"/>,而不是這個列舉的某個值;
/// 交易所給了本列舉不認得的原因時才是 <see cref="Other"/>。
/// When the exchange gives no reason, <see cref="Order.ExpiryReason"/> is <see langword="null"/> rather than any
/// value of this enumeration; <see cref="Other"/> is reserved for a reason the exchange gave that this enumeration
/// does not recognise.
/// </para>
/// </remarks>
public enum OrderExpiryReason
{
    /// <summary>
    /// 交易所給了本列舉未涵蓋的原因。
    /// The exchange gave a reason this enumeration does not cover.
    /// </summary>
    Other = 0,

    /// <summary>
    /// 為了防止自成交(同一帳戶自己跟自己成交)而讓這張單到期。
    /// The order expired to prevent the account from trading against itself.
    /// </summary>
    SelfTradePrevention = 1,

    /// <summary>
    /// <c>IOC</c> 沒有全部成交,剩下的數量被取消。
    /// An <c>IOC</c> order did not fill completely and the remainder was cancelled.
    /// </summary>
    ImmediateOrCancelRemainder = 2,

    /// <summary>
    /// <c>IOC</c> 為了防止自成交而沒有全部成交,剩下的數量被取消。
    /// An <c>IOC</c> order stopped short of filling to prevent self-trading, and the remainder was cancelled.
    /// </summary>
    ImmediateOrCancelSelfTradePrevention = 3,

    /// <summary>
    /// 被另一張優先權更高的只減倉(市價)單擠掉,或繼續保留會開出反向部位。
    /// Knocked out by a higher-priority reduce-only (market) order, or keeping it would have opened an opposite
    /// position.
    /// </summary>
    ReduceOnlyKnockout = 4,

    /// <summary>
    /// 帳戶被強制平倉時到期。
    /// The order expired while the account was being liquidated.
    /// </summary>
    Liquidation = 5,

    /// <summary>
    /// 只在持有部位時有效的條件(例如幣安的 <c>GTE</c>)不再成立,通常是部位已經歸零。
    /// A condition valid only while a position is held — Binance's <c>GTE</c>, for instance — stopped holding,
    /// usually because the position went flat.
    /// </summary>
    PositionConditionUnsatisfied = 6,

    /// <summary>
    /// 商品下架而被取消。
    /// The order was cancelled because the symbol was delisted.
    /// </summary>
    SymbolDelisted = 7,

    /// <summary>
    /// 停損單觸發之後,原本那張委託到期。
    /// The original order expired once a stop order triggered.
    /// </summary>
    StopTriggered = 8,

    /// <summary>
    /// 市價單沒有全部成交,剩下的數量被取消。
    /// A market order did not fill completely and the remainder was cancelled.
    /// </summary>
    MarketOrderRemainder = 9,
}
