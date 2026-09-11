namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 需要重新對帳的原因。
/// Why a full reconciliation is needed.
/// </summary>
public enum ResyncReason
{
    /// <summary>
    /// 交易所給了本列舉未涵蓋的原因。
    /// The exchange gave a reason this enumeration does not cover.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 私有串流的憑證失效(如幣安的 listenKey 過期),串流已經斷開。
    /// The private stream's credential expired — a Binance listenKey, for instance — and the stream is gone.
    /// </summary>
    StreamCredentialExpired = 1,

    /// <summary>
    /// 斷線後重新連上。重連期間發生的事件不會補送。
    /// The stream reconnected after dropping. Nothing that happened while it was down is replayed.
    /// </summary>
    Reconnected = 2,

    /// <summary>
    /// 偵測到事件序號不連續,中間漏了東西。
    /// A gap was detected in the event sequence: something in the middle went missing.
    /// </summary>
    EventGap = 3,
}
