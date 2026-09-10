using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 查詢歷史 K 線的條件。
/// The criteria for a historical kline query.
/// </summary>
/// <remarks>
/// 時間區間為左閉右開:<see cref="StartTime"/> 那一刻開盤的 K 線會被納入,<see cref="EndTime"/> 那一刻
/// 開盤的不會。這樣連續分頁抓取才不會在邊界重複拿到同一根。
/// The range is half-open: the candle opening exactly at <see cref="StartTime"/> is included while the one opening
/// at <see cref="EndTime"/> is not, so paging through history never returns the boundary candle twice.
/// </remarks>
public sealed record KlineQuery
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
    /// 起始時間(含,UTC 語意)。未指定時由交易所決定。
    /// The inclusive start time in UTC semantics; the exchange decides when it is unset.
    /// </summary>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    /// 結束時間(不含,UTC 語意)。未指定時取到最新。
    /// The exclusive end time in UTC semantics; the query runs to the latest candle when it is unset.
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>
    /// 最多回傳幾根。未指定時由實作決定,實作也可能受交易所單次上限限制。
    /// The maximum number of candles. The implementation decides when unset, and may be capped by the exchange.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// 檢查查詢條件是否合法。
    /// Checks that the query is valid.
    /// </summary>
    /// <returns>
    /// 合法時為成功,否則為驗證失敗。
    /// Success when valid; otherwise a validation failure.
    /// </returns>
    public Result Validate()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            return TradeErrors.InvalidQuery("交易對代碼不可為空白。The symbol must not be blank.");
        }

        if (Interval == KlineInterval.Unspecified)
        {
            return TradeErrors.UnsupportedInterval(null);
        }

        if (StartTime is { } start && EndTime is { } end && start >= end)
        {
            return TradeErrors.InvalidQuery("起始時間必須早於結束時間。The start time must be earlier than the end time.");
        }

        return Limit is <= 0
            ? TradeErrors.InvalidQuery("筆數上限必須大於零。The limit must be greater than zero.")
            : Result.Success();
    }
}
