using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 單一資產的合約帳戶餘額。
/// The derivatives account balance of one asset.
/// </summary>
public sealed record Balance
{
    /// <summary>
    /// 資產代碼,例如 <c>USDT</c>。
    /// The asset code such as <c>USDT</c>.
    /// </summary>
    public required string Asset { get; init; }

    /// <summary>
    /// 錢包餘額,不含未實現損益。
    /// The wallet balance, excluding unrealised profit and loss.
    /// </summary>
    public decimal WalletBalance { get; init; }

    /// <summary>
    /// 可用餘額:扣掉保證金與掛單凍結之後,還能拿來開新倉的部分。
    /// The available balance left for new positions after margin and open-order reservations.
    /// </summary>
    public decimal AvailableBalance { get; init; }

    /// <summary>
    /// 這個資產目前的未實現損益。
    /// The unrealised profit and loss carried by this asset.
    /// </summary>
    public decimal UnrealizedPnl { get; init; }

    /// <summary>
    /// 保證金餘額,等於 <see cref="WalletBalance"/> 加上 <see cref="UnrealizedPnl"/>。
    /// The margin balance, equal to <see cref="WalletBalance"/> plus <see cref="UnrealizedPnl"/>.
    /// </summary>
    /// <remarks>
    /// 判斷離爆倉還有多遠要看這個數字,不是看錢包餘額 —— 浮虧會先吃掉保證金,錢包餘額卻要等平倉才變動。
    /// This is the number that says how far the account is from liquidation, not the wallet balance: an open loss
    /// eats the margin immediately while the wallet balance only moves when the position closes.
    /// </remarks>
    public decimal MarginBalance => WalletBalance + UnrealizedPnl;

    /// <summary>
    /// 這個資產目前的維持保證金:保證金餘額低於它就會被強制平倉。交易所未提供時為 <see langword="null"/>。
    /// The maintenance margin currently required on this asset — liquidation starts once the margin balance falls
    /// below it; <see langword="null"/> when the exchange does not provide it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 保證金率(<see cref="MarginBalance"/> ÷ 維持保證金)要用的是這個數字,不是起始保證金。
    /// 拿起始保證金代替,算出來的保證金率會偏低,風控會比實際需要更早擋單。
    /// This is the figure a margin ratio (<see cref="MarginBalance"/> ÷ maintenance margin) needs, not the initial
    /// margin. Substituting the initial margin understates the ratio and makes risk control block orders earlier
    /// than it has to.
    /// </para>
    /// <para>
    /// <b><see langword="null"/> 與 0 意思不同。</b>0 是交易所明確回報「目前沒有維持保證金需求」(例如空手);
    /// <see langword="null"/> 是「不知道」。實作端拿不到這個值時必須留 <see langword="null"/>,不可填 0 ——
    /// 填 0 等於告訴風控這個帳戶沒有任何強平風險。
    /// <b><see langword="null"/> and zero mean different things.</b> Zero is the exchange stating that no
    /// maintenance margin is required right now, for instance when flat; <see langword="null"/> means unknown.
    /// An implementation that cannot obtain the value must leave it <see langword="null"/> rather than zero, because
    /// zero tells risk control the account carries no liquidation risk at all.
    /// </para>
    /// </remarks>
    public decimal? MaintenanceMargin { get; init; }

    /// <summary>
    /// 這個資產目前佔用的起始保證金(持倉與掛單合計)。交易所未提供時為 <see langword="null"/>。
    /// The initial margin currently committed on this asset, positions and open orders together;
    /// <see langword="null"/> when the exchange does not provide it.
    /// </summary>
    /// <remarks>
    /// 與 <see cref="MaintenanceMargin"/> 相同,<see langword="null"/> 表示未提供,0 表示交易所明確回報為零。
    /// As with <see cref="MaintenanceMargin"/>, <see langword="null"/> means not provided and zero means the
    /// exchange reported zero.
    /// </remarks>
    public decimal? InitialMargin { get; init; }

    /// <summary>
    /// 把錢包餘額轉成帶幣別的金額,交給需要防止跨幣別誤算的地方使用。
    /// Converts the wallet balance into a currency-aware amount for code that must not mix currencies.
    /// </summary>
    /// <returns>帶幣別的錢包餘額。The wallet balance with its currency attached.</returns>
    /// <exception cref="ArgumentException">
    /// <see cref="Asset"/> 為空白時擲出。
    /// Thrown when <see cref="Asset"/> is blank.
    /// </exception>
    public Money ToMoney() => new(WalletBalance, Asset);

    /// <summary>
    /// 把可用餘額轉成帶幣別的金額。
    /// Converts the available balance into a currency-aware amount.
    /// </summary>
    /// <returns>帶幣別的可用餘額。The available balance with its currency attached.</returns>
    /// <exception cref="ArgumentException">
    /// <see cref="Asset"/> 為空白時擲出。
    /// Thrown when <see cref="Asset"/> is blank.
    /// </exception>
    public Money AvailableAsMoney() => new(AvailableBalance, Asset);
}
