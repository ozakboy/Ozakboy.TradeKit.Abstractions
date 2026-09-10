namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 標記價更新。標記價是合約計算未實現損益與強平的基準價。
/// A mark price update. The mark price is the reference used for unrealised PnL and liquidation.
/// </summary>
/// <remarks>
/// 標記價不等於最新成交價:它由指數價與資金費率推算,目的就是讓單一筆極端成交無法把整個市場的部位掃掉。
/// 停損的觸發判斷、風控的部位估值都以這個價格為準。
/// The mark price is not the last traded price: it is derived from the index and funding rate precisely so that a
/// single extreme trade cannot wipe out the market's positions. Stop triggers and risk valuations use it.
/// </remarks>
public sealed record MarkPriceUpdate
{
    /// <summary>
    /// 交易對代碼。
    /// The symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 標記價。
    /// The mark price.
    /// </summary>
    public required decimal MarkPrice { get; init; }

    /// <summary>
    /// 指數價。交易所未提供時為 <see langword="null"/>。
    /// The index price, or <see langword="null"/> when the exchange does not provide it.
    /// </summary>
    public decimal? IndexPrice { get; init; }

    /// <summary>
    /// 目前的資金費率。永續合約以外的商品為 <see langword="null"/>。
    /// The current funding rate, or <see langword="null"/> for non-perpetual instruments.
    /// </summary>
    public decimal? FundingRate { get; init; }

    /// <summary>
    /// 下次結算資金費用的時間(UTC 語意)。
    /// The next funding settlement time, in UTC semantics.
    /// </summary>
    public DateTimeOffset? NextFundingTime { get; init; }

    /// <summary>
    /// 這筆更新的時間(UTC 語意)。
    /// The timestamp of this update, in UTC semantics.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}
