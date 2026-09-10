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
