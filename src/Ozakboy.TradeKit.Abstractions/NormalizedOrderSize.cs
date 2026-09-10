namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 經過交易規則校正、確定可以送出的價格與數量。
/// A price and quantity that have passed the symbol's trading rules and are safe to send.
/// </summary>
/// <param name="Price">
/// 已對齊最小跳動點的價格。
/// The price, aligned to the tick size.
/// </param>
/// <param name="Quantity">
/// 已向下對齊最小數量步進的數量。
/// The quantity, aligned down to the step size.
/// </param>
/// <param name="Notional">
/// 名目價值,等於 <paramref name="Price"/> × <paramref name="Quantity"/>。
/// The notional value, equal to <paramref name="Price"/> times <paramref name="Quantity"/>.
/// </param>
public readonly record struct NormalizedOrderSize(decimal Price, decimal Quantity, decimal Notional);
