namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 帳戶的一次變動:這次動到的餘額與部位。
/// One change to an account: the balances and positions it touched.
/// </summary>
/// <remarks>
/// <para>
/// <b>這是增量,不是快照。</b><see cref="Balances"/> 與 <see cref="Positions"/> 只列這次有變動的項目,
/// 沒有變動的完全不會出現。把它當成 <see cref="AccountSnapshot"/> 來用,等於把「沒被提到的餘額」
/// 誤讀成零 —— 一個 USDT 帳戶會在某次只動到部位的更新之後,突然看起來像被清空了。
/// <b>This is a delta, not a snapshot.</b> <see cref="Balances"/> and <see cref="Positions"/> carry only what
/// changed; anything untouched is simply absent. Reading it as an <see cref="AccountSnapshot"/> turns "not
/// mentioned" into "zero" — and a USDT account appears to have been emptied by an update that only moved a
/// position.
/// </para>
/// <para>
/// 型別之所以與 <see cref="AccountSnapshot"/> 分開,就是為了讓這個差別在型別上擋住,而不是寫在文件裡祈禱。
/// The separate type exists so that the difference is caught by the compiler rather than documented and hoped for.
/// </para>
/// </remarks>
public sealed record AccountUpdate
{
    /// <summary>
    /// 這次變動的原因。
    /// What caused this change.
    /// </summary>
    public AccountUpdateReason Reason { get; init; } = AccountUpdateReason.Unknown;

    /// <summary>
    /// 交易所給的原始原因代碼,保留原樣。本列舉沒收到的代碼可以從這裡看出到底是什麼。
    /// The exchange's own reason code, kept verbatim, so a code the enumeration does not cover can still be seen.
    /// </summary>
    public string? RawReason { get; init; }

    /// <summary>
    /// 這次<b>有變動</b>的餘額。型別只含事件帶得來的欄位,見 <see cref="BalanceChange"/>。
    /// The balances that <b>changed</b>; the type holds only what the event delivers — see
    /// <see cref="BalanceChange"/>.
    /// </summary>
    public IReadOnlyList<BalanceChange> Balances { get; init; } = [];

    /// <summary>
    /// 這次<b>有變動</b>的部位。型別刻意沒有標記價與名目價值,見 <see cref="PositionChange"/>。
    /// The positions that <b>changed</b>; the type deliberately has no mark price or notional — see
    /// <see cref="PositionChange"/>.
    /// </summary>
    public IReadOnlyList<PositionChange> Positions { get; init; } = [];

    /// <summary>
    /// 這筆更新的時間(UTC 語意)。對應交易所推送這則事件的<b>事件時間</b>,不是帳戶實際變動的時刻。
    /// The timestamp of this update, in UTC semantics. It is the exchange's <b>event time</b> — when the event was
    /// pushed — rather than when the account actually changed.
    /// </summary>
    /// <remarks>
    /// 與 <see cref="TransactionTime"/> 的差別見該成員的說明。
    /// See <see cref="TransactionTime"/> for how the two differ.
    /// </remarks>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// 交易所的撮合 / 交易時間(UTC 語意):帳戶這次變動實際發生的時刻;交易所沒有提供時為 <see langword="null"/>。
    /// The exchange's matching or transaction time, in UTC semantics — when this change to the account actually
    /// happened — or <see langword="null"/> when the exchange does not provide one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>與 <see cref="Timestamp"/> 不同。</b><see cref="Timestamp"/> 是交易所<b>推送事件</b>的時間,
    /// 這個成員是帳戶<b>變動發生</b>的時間;前者通常等於或晚於後者,兩者相差的是交易所內部排隊與推送的延遲。
    /// <b>It differs from <see cref="Timestamp"/>.</b> <see cref="Timestamp"/> is when the exchange <b>pushed the
    /// event</b>; this member is when the account <b>changed</b>. The former is usually equal to or later than the
    /// latter, the gap being the exchange's own queueing and delivery delay.
    /// </para>
    /// <para>
    /// <b>排序與新舊比較請用這個成員。</b>判斷「另一份資料(例如 REST 的部位快照)是不是比這次變動還舊」時,
    /// 該比的是變動發生的時刻,而不是事件推送的時刻 —— 推送延遲一大,拿事件時間比就可能把一份其實已經
    /// 反映這次變動的快照誤判成過期,或反過來。
    /// <b>Use this member for ordering and staleness.</b> Deciding whether another piece of data — a REST position
    /// snapshot, for instance — predates this change means comparing against when the change happened, not when
    /// the event was pushed. With a large delivery delay, comparing event times can call a snapshot that already
    /// reflects this change stale, or the other way round.
    /// </para>
    /// <para>
    /// <b>拿不到就是 <see langword="null"/>,不猜。</b>交易所沒有給這個時間時,實作不可以拿 <see cref="Timestamp"/>
    /// 或本機時鐘代填:代填之後「交易所說的時間」與「推測出來的時間」看起來一模一樣,用它做的新舊判斷
    /// 就會在沒有任何跡象的情況下出錯。消費端遇到 <see langword="null"/> 應該明確決定退回什麼依據。
    /// <b>Missing means <see langword="null"/>; nothing is guessed.</b> When the exchange does not provide the time,
    /// an implementation must not fill it from <see cref="Timestamp"/> or the local clock: once filled, "what the
    /// exchange said" and "what was inferred" look identical, and every staleness decision built on it goes wrong
    /// without a trace. A consumer meeting <see langword="null"/> should decide explicitly what to fall back on.
    /// </para>
    /// </remarks>
    public DateTimeOffset? TransactionTime { get; init; }
}
