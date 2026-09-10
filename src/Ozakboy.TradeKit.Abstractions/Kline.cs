namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 一根 K 線(蠟燭)。
/// One candlestick.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IsClosed"/> 是這個型別最重要的欄位。即時串流會不斷推送「還在跳動」的當前這根 K 線,
/// 拿未收盤的 K 線去算指標會得到每秒都在變的訊號,回測完全重現不了。策略一律只吃
/// <see cref="IsClosed"/> 為 <see langword="true"/> 的 K 線。
/// <see cref="IsClosed"/> is the most important field here. A live stream keeps pushing the in-progress candle,
/// and feeding an unfinished candle into an indicator produces a signal that changes every second and cannot be
/// reproduced in a backtest. Strategies consume only candles where <see cref="IsClosed"/> is <see langword="true"/>.
/// </para>
/// <para>
/// 時間欄位的語意為 UTC。
/// The timestamps are in UTC semantics.
/// </para>
/// </remarks>
public sealed record Kline
{
    /// <summary>
    /// 交易對代碼。
    /// The symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// K 線週期。
    /// The interval.
    /// </summary>
    public required KlineInterval Interval { get; init; }

    /// <summary>
    /// 開盤時間(UTC 語意)。
    /// The open time, in UTC semantics.
    /// </summary>
    public required DateTimeOffset OpenTime { get; init; }

    /// <summary>
    /// 收盤時間(UTC 語意)。
    /// The close time, in UTC semantics.
    /// </summary>
    public required DateTimeOffset CloseTime { get; init; }

    /// <summary>
    /// 開盤價。
    /// The opening price.
    /// </summary>
    public required decimal Open { get; init; }

    /// <summary>
    /// 最高價。
    /// The highest price.
    /// </summary>
    public required decimal High { get; init; }

    /// <summary>
    /// 最低價。
    /// The lowest price.
    /// </summary>
    public required decimal Low { get; init; }

    /// <summary>
    /// 收盤價。未收盤時為目前的最新價。
    /// The closing price, or the latest price while the candle is still open.
    /// </summary>
    public required decimal Close { get; init; }

    /// <summary>
    /// 成交量,以基礎幣計。
    /// The traded volume in base asset units.
    /// </summary>
    public decimal Volume { get; init; }

    /// <summary>
    /// 成交額,以計價幣計。
    /// The traded turnover in quote asset units.
    /// </summary>
    public decimal QuoteVolume { get; init; }

    /// <summary>
    /// 這根 K 線的成交筆數。
    /// The number of trades in this candle.
    /// </summary>
    public int TradeCount { get; init; }

    /// <summary>
    /// 這根 K 線是否已經收盤。
    /// Whether this candle has closed.
    /// </summary>
    public bool IsClosed { get; init; }

    /// <summary>
    /// 高低價差。
    /// The high-to-low range.
    /// </summary>
    public decimal Range => High - Low;

    /// <summary>
    /// 收盤價是否高於開盤價。
    /// Whether the close is above the open.
    /// </summary>
    public bool IsBullish => Close > Open;

    /// <summary>
    /// 典型價格,等於(最高價 + 最低價 + 收盤價)÷ 3。
    /// The typical price, equal to high plus low plus close, divided by three.
    /// </summary>
    public decimal TypicalPrice => (High + Low + Close) / 3m;
}
