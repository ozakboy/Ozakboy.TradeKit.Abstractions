namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 條件單的一次狀態變化,由私有串流送出。
/// One state change of a conditional order, as delivered by the private stream.
/// </summary>
/// <remarks>
/// <para>
/// 委託更新直接送 <see cref="Order"/>,條件單卻多包一層,是因為這個事件帶得出快照帶不出的東西:
/// 交易所自己的狀態字串(<see cref="RawStatus"/>)與拒絕原因(<see cref="RejectReason"/>)。
/// 停損被交易所拒絕是會出人命的事 —— 以為有保護、其實沒有 —— 而拒絕原因只在事件裡出現一次,
/// 之後再查那張單只會得到一個「已拒絕」,看不出為什麼。
/// Order updates deliver a bare <see cref="Order"/>, while conditional orders get a wrapper, because this event
/// carries what a snapshot cannot: the exchange's own status string (<see cref="RawStatus"/>) and the reason for
/// a rejection (<see cref="RejectReason"/>). A stop rejected by the exchange is the dangerous case — protection
/// believed to be in place that is not — and the reason appears exactly once, in this event; looking the order up
/// afterwards yields a bare "rejected" with no explanation.
/// </para>
/// <para>
/// <see cref="ConditionalOrder"/> 本身是<b>當下的完整狀態</b>,不是增量,可以直接覆蓋本地的那一份。
/// The <see cref="ConditionalOrder"/> inside is the <b>complete current state</b> rather than a delta, and can
/// overwrite the local copy directly.
/// </para>
/// </remarks>
public sealed record ConditionalOrderUpdate
{
    /// <summary>
    /// 變化之後的條件單狀態。
    /// The conditional order as it stands after the change.
    /// </summary>
    public required ConditionalOrder ConditionalOrder { get; init; }

    /// <summary>
    /// 交易所給的原始狀態字串,保留原樣。<see cref="ConditionalOrderStatus"/> 沒收到的值可以從這裡看出
    /// 到底是什麼。
    /// The exchange's own status string, kept verbatim, so a value that
    /// <see cref="ConditionalOrderStatus"/> does not cover can still be seen.
    /// </summary>
    public string? RawStatus { get; init; }

    /// <summary>
    /// 被拒絕的原因,交易所有給才有值。
    /// Why it was rejected, when the exchange supplies a reason.
    /// </summary>
    public string? RejectReason { get; init; }

    /// <summary>
    /// 這筆更新的時間(UTC 語意)。
    /// The timestamp of this update, in UTC semantics.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}
