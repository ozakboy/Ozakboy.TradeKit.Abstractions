namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 「本地狀態已經不可信,請全量對帳」的訊號。
/// The signal that local state can no longer be trusted and must be reconciled in full.
/// </summary>
/// <remarks>
/// <para>
/// 私有串流一旦中斷過,期間發生的委託與成交就不會再送來 —— 交易所不補送,本地的部位與訂單狀態
/// 從那一刻起就與交易所分岔了,而且<b>看不出來</b>:帳面上的部位數字依然是個合理的數字。
/// 收到這個訊號的正確反應是重查一次帳戶與未結委託,用交易所的答案覆蓋本地的。
/// Once a private stream has been interrupted, the orders and fills from that interval are never delivered — the
/// exchange does not replay them — and local positions and order state diverge from the exchange's from that
/// moment on, <b>invisibly</b>: the position on the books remains a perfectly plausible number. The correct
/// response is to re-read the account and the open orders, and let the exchange's answer overwrite the local one.
/// </para>
/// <para>
/// 這個訊號刻意不綁在任何一家交易所的名詞上。幣安的 <c>listenKeyExpired</c> 是它的來源之一,
/// 但<b>斷線重連</b>造成的後果一模一樣,而且常見得多 —— 兩者走同一條訊號,消費端就不會只處理了罕見的那一個。
/// The signal is deliberately not tied to any one exchange's vocabulary. A Binance <c>listenKeyExpired</c> is one
/// source of it, but a plain <b>reconnect</b> has identical consequences and happens far more often; routing both
/// through one signal keeps a consumer from handling only the rarer of the two.
/// </para>
/// </remarks>
public sealed record ResyncRequired
{
    /// <summary>
    /// 需要重新對帳的原因。
    /// Why the reconciliation is needed.
    /// </summary>
    public ResyncReason Reason { get; init; } = ResyncReason.Unknown;

    /// <summary>
    /// 給人看的補充說明,例如斷線持續了多久。
    /// A human-readable detail, such as how long the stream was down.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// 本地狀態<b>從這個時刻起</b>不可信(UTC 語意)。通常是串流斷開的時間,而不是發現的時間。
    /// Local state is untrustworthy <b>from this moment on</b>, in UTC semantics: normally when the stream
    /// dropped, not when that was noticed.
    /// </summary>
    public DateTimeOffset UntrustedSince { get; init; }

    /// <summary>
    /// 這個訊號送出的時間(UTC 語意)。
    /// When this signal was raised, in UTC semantics.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}
