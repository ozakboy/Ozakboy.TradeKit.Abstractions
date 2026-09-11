namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 帳戶變動的原因。
/// What caused an account to change.
/// </summary>
/// <remarks>
/// <para>
/// 交易所各自有一長串原因代碼,這裡只收有語意分量、消費端真的會分開處理的那幾類;
/// 認不出來的一律落在 <see cref="Unknown"/>,因此交易所日後新增代碼不會讓解析失敗。
/// Exchanges each publish a long list of reason codes; only those carrying enough meaning for a consumer to act
/// on differently appear here, and anything unrecognised lands on <see cref="Unknown"/>, so a code added later
/// by an exchange does not break parsing.
/// </para>
/// <para>
/// 分辨這幾類不是為了好看:年度稅務彙總要把資金費與成交損益拆開,而入金出金根本不是損益,
/// 混在一起算會把權益曲線畫成一條假的上升線。
/// The distinction earns its keep: a yearly tax summary has to separate funding from trading PnL, and deposits
/// are not PnL at all — folding them in draws an equity curve that rises for reasons the strategy never earned.
/// </para>
/// </remarks>
public enum AccountUpdateReason
{
    /// <summary>
    /// 交易所給了本列舉未涵蓋的原因。
    /// The exchange gave a reason this enumeration does not cover.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 委託成交造成的變動。
    /// The change came from an order filling.
    /// </summary>
    Order = 1,

    /// <summary>
    /// 永續合約的資金費結算。
    /// A perpetual contract's funding settlement.
    /// </summary>
    FundingFee = 2,

    /// <summary>
    /// 入金。
    /// A deposit.
    /// </summary>
    Deposit = 3,

    /// <summary>
    /// 出金。
    /// A withdrawal.
    /// </summary>
    Withdrawal = 4,

    /// <summary>
    /// 保證金在錢包與部位之間轉移(逐倉加減保證金)。
    /// Margin moved between the wallet and a position, as when adding to or removing from isolated margin.
    /// </summary>
    MarginTransfer = 5,

    /// <summary>
    /// 保證金模式切換(全倉與逐倉互換)。
    /// The margin mode was switched between cross and isolated.
    /// </summary>
    MarginModeChange = 6,

    /// <summary>
    /// 強制平倉或保險基金清算。
    /// A forced liquidation or an insurance-fund clearance.
    /// </summary>
    Liquidation = 7,

    /// <summary>
    /// 交易所主動調整餘額。
    /// The exchange adjusted the balance on its own initiative.
    /// </summary>
    Adjustment = 8,

    /// <summary>
    /// 資產之間的自動兌換或轉換。
    /// An automatic exchange or conversion between assets.
    /// </summary>
    AssetConversion = 9,
}
