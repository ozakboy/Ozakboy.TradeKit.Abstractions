namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 帳戶變動事件裡的一筆餘額增量:只含事件真的帶來的欄位。
/// One balance entry inside an account change event, carrying only what the event actually delivers.
/// </summary>
/// <remarks>
/// <b>刻意沒有可用餘額與未實現損益。</b>交易所的帳戶變動事件不帶這兩個值;先前用完整的 <see cref="Balance"/>
/// 時它們只能填 0,於是 <see cref="Balance.MarginBalance"/> 會悄悄等於錢包餘額。下單前要知道還能動用多少,
/// 請查帳戶快照。
/// <b>There is deliberately no available balance or unrealised PnL.</b> Account change events do not carry them;
/// with a full <see cref="Balance"/> they had to be zero, and <see cref="Balance.MarginBalance"/> quietly equalled
/// the wallet balance. To know what can be committed before placing an order, query an account snapshot.
/// </remarks>
public sealed record BalanceChange
{
    /// <summary>
    /// 資產代碼,例如 <c>USDT</c>。
    /// The asset code, such as <c>USDT</c>.
    /// </summary>
    public required string Asset { get; init; }

    /// <summary>
    /// 變動後的錢包餘額。
    /// The wallet balance after the change.
    /// </summary>
    public decimal WalletBalance { get; init; }

    /// <summary>
    /// 變動後的全倉錢包餘額。交易所未提供時為 <see langword="null"/>。
    /// The cross wallet balance after the change, or <see langword="null"/> when not provided.
    /// </summary>
    public decimal? CrossWalletBalance { get; init; }

    /// <summary>
    /// 交易以外的餘額變動量(入金、出金、轉帳),不含損益與手續費。交易所未提供時為 <see langword="null"/>。
    /// The balance change from anything other than trading — deposits, withdrawals, transfers — excluding PnL and
    /// fees; <see langword="null"/> when not provided.
    /// </summary>
    /// <remarks>
    /// 權益曲線要扣掉的就是這一項:入金讓餘額變多,但那不是策略賺的。
    /// This is what an equity curve has to take out: a deposit raises the balance, and the strategy did not earn it.
    /// </remarks>
    public decimal? NonTradingChange { get; init; }
}
