using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 合約帳戶在某一時刻的完整快照:餘額與持倉。
/// A complete snapshot of the derivatives account at one instant: balances and positions.
/// </summary>
/// <remarks>
/// 刻意做成一次取回的快照,而不是分開查餘額與持倉。分開查會拿到兩個時間點的資料,中間只要有一筆成交,
/// 算出來的保證金使用率就是錯的,而且錯得沒有規律、事後也無法重現。
/// This is deliberately one snapshot rather than separate balance and position queries. Fetching them separately
/// gives two different instants, and a single fill in between makes the computed margin usage wrong in a way that
/// is neither consistent nor reproducible afterwards.
/// </remarks>
public sealed record AccountSnapshot
{
    /// <summary>
    /// 各資產的餘額。
    /// The balance of each asset.
    /// </summary>
    public IReadOnlyList<Balance> Balances { get; init; } = [];

    /// <summary>
    /// 目前的持倉。空手的商品可以不列入。
    /// The current positions. Flat symbols may be omitted.
    /// </summary>
    public IReadOnlyList<Position> Positions { get; init; } = [];

    /// <summary>
    /// 是否為雙向(避險)持倉模式。
    /// Whether the account is in hedge mode.
    /// </summary>
    public bool IsHedgeMode { get; init; }

    /// <summary>
    /// 是否可以交易(帳戶被限制時為 <see langword="false"/>)。
    /// Whether the account may trade; <see langword="false"/> when the exchange has restricted it.
    /// </summary>
    public bool CanTrade { get; init; } = true;

    /// <summary>
    /// 整個帳戶的維持保證金總額。交易所未提供時為 <see langword="null"/>。
    /// The maintenance margin required across the whole account; <see langword="null"/> when the exchange does not
    /// provide it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 以交易所彙總帳戶時使用的計價單位表示,不一定等於任何單一資產的 <see cref="Balance.MaintenanceMargin"/>
    /// —— 多資產保證金模式下,交易所會把各資產換算成同一個單位再加總。只看單一保證金資產時,
    /// 請改用那個資產的 <see cref="Balance.MaintenanceMargin"/>。
    /// Expressed in whatever unit the exchange aggregates the account in, which need not match any single asset's
    /// <see cref="Balance.MaintenanceMargin"/>: in a multi-asset margin mode the exchange converts every asset into
    /// one unit before summing. For a single margin asset, use that asset's <see cref="Balance.MaintenanceMargin"/>.
    /// </para>
    /// <para>
    /// <b><see langword="null"/> 與 0 意思不同。</b>0 是交易所明確回報「沒有維持保證金需求」;
    /// <see langword="null"/> 是「不知道」,風控不可把它當成沒有強平風險。
    /// <b><see langword="null"/> and zero mean different things.</b> Zero is the exchange stating that no
    /// maintenance margin is required; <see langword="null"/> means unknown, and risk control must not read it as
    /// the absence of liquidation risk.
    /// </para>
    /// </remarks>
    public decimal? TotalMaintenanceMargin { get; init; }

    /// <summary>
    /// 整個帳戶佔用的起始保證金總額(持倉與掛單合計)。交易所未提供時為 <see langword="null"/>。
    /// The initial margin committed across the whole account, positions and open orders together;
    /// <see langword="null"/> when the exchange does not provide it.
    /// </summary>
    /// <remarks>
    /// 計價單位與 <see langword="null"/> 的語意同 <see cref="TotalMaintenanceMargin"/>。
    /// The unit and the meaning of <see langword="null"/> are as for <see cref="TotalMaintenanceMargin"/>.
    /// </remarks>
    public decimal? TotalInitialMargin { get; init; }

    /// <summary>
    /// 快照時間(UTC 語意)。
    /// The time of the snapshot, in UTC semantics.
    /// </summary>
    public DateTimeOffset TakenAt { get; init; }

    /// <summary>
    /// 取得指定資產的餘額。
    /// Gets the balance of one asset.
    /// </summary>
    /// <param name="asset">資產代碼,比對時不分大小寫。The asset code; the comparison ignores case.</param>
    /// <returns>
    /// 找到時為該筆餘額,否則為代碼 <see cref="TradeErrorCodes.BalanceNotFound"/> 的失敗。
    /// The balance when present; otherwise a failure carrying <see cref="TradeErrorCodes.BalanceNotFound"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="asset"/> 為 <see langword="null"/> 時擲出。
    /// Thrown when <paramref name="asset"/> is <see langword="null"/>.
    /// </exception>
    public Result<Balance> GetBalance(string asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        foreach (var balance in Balances)
        {
            if (string.Equals(balance.Asset, asset, StringComparison.OrdinalIgnoreCase))
            {
                return balance;
            }
        }

        return TradeErrors.BalanceNotFound(asset);
    }

    /// <summary>
    /// 取得指定商品的持倉。
    /// Gets the position on one symbol.
    /// </summary>
    /// <param name="symbol">交易對代碼,比對時不分大小寫。The symbol; the comparison ignores case.</param>
    /// <returns>
    /// 找到時為該筆持倉,否則為代碼 <see cref="TradeErrorCodes.PositionNotFound"/> 的失敗。
    /// The position when present; otherwise a failure carrying <see cref="TradeErrorCodes.PositionNotFound"/>.
    /// </returns>
    /// <remarks>
    /// 「查無持倉」與「持倉為零」在交易所回應裡是同一件事,但兩者都不該讓呼叫端拿到 <see langword="null"/>。
    /// 需要一個一定拿得到的值時,請用 <see cref="GetPositionOrFlat"/>。
    /// "No position" and "zero position" are the same thing in an exchange response, and neither should hand the
    /// caller a <see langword="null"/>. Use <see cref="GetPositionOrFlat"/> when a value is always wanted.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="symbol"/> 為 <see langword="null"/> 時擲出。
    /// Thrown when <paramref name="symbol"/> is <see langword="null"/>.
    /// </exception>
    public Result<Position> GetPosition(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        foreach (var position in Positions)
        {
            if (string.Equals(position.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
            {
                return position;
            }
        }

        return TradeErrors.PositionNotFound(symbol);
    }

    /// <summary>
    /// 取得指定商品的持倉,查無資料時回傳空手的持倉。
    /// Gets the position on one symbol, returning a flat position when none is present.
    /// </summary>
    /// <param name="symbol">交易對代碼,比對時不分大小寫。The symbol; the comparison ignores case.</param>
    /// <returns>持倉,或數量為零的持倉。The position, or a flat one.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="symbol"/> 為空白時擲出。
    /// Thrown when <paramref name="symbol"/> is blank.
    /// </exception>
    public Position GetPositionOrFlat(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        return GetPosition(symbol).GetValueOrDefault(Position.Flat(symbol, TakenAt));
    }
}
