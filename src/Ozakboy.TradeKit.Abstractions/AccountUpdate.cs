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
    /// 這次<b>有變動</b>的餘額。
    /// The balances that <b>changed</b>.
    /// </summary>
    public IReadOnlyList<Balance> Balances { get; init; } = [];

    /// <summary>
    /// 這次<b>有變動</b>的部位。
    /// The positions that <b>changed</b>.
    /// </summary>
    public IReadOnlyList<Position> Positions { get; init; } = [];

    /// <summary>
    /// 這筆更新的時間(UTC 語意)。
    /// The timestamp of this update, in UTC semantics.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}
