namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 條件單以哪一種價格判斷是否觸發。
/// Which price a conditional order watches to decide whether it triggers.
/// </summary>
/// <remarks>
/// 停損單預設看標記價。看最新成交價會被瞬間的插針行情掃出場 —— 那一筆成交可能只有極小的量,
/// 標記價則是由指數價與資金費率推算,不容易被單一筆交易帶動。
/// Stops default to the mark price. Watching the last traded price gets you stopped out by a momentary wick that
/// may carry almost no volume, whereas the mark price is derived from the index and funding rate and is far harder
/// for a single trade to move.
/// </remarks>
public enum TriggerPriceType
{
    /// <summary>
    /// 標記價(預設)。
    /// The mark price, which is the default.
    /// </summary>
    MarkPrice = 0,

    /// <summary>
    /// 最新成交價。
    /// The last traded price.
    /// </summary>
    LastPrice = 1,
}
