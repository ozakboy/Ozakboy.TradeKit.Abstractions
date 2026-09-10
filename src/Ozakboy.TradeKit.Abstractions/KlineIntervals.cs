using System.Collections.Frozen;

using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// <see cref="KlineInterval"/> 的解析與列舉。
/// Parsing and enumeration for <see cref="KlineInterval"/>.
/// </summary>
public static class KlineIntervals
{
    private static readonly KlineInterval[] Supported =
    [
        KlineInterval.OneMinute,
        KlineInterval.ThreeMinutes,
        KlineInterval.FiveMinutes,
        KlineInterval.FifteenMinutes,
        KlineInterval.ThirtyMinutes,
        KlineInterval.OneHour,
        KlineInterval.TwoHours,
        KlineInterval.FourHours,
        KlineInterval.SixHours,
        KlineInterval.EightHours,
        KlineInterval.TwelveHours,
        KlineInterval.OneDay,
        KlineInterval.ThreeDays,
        KlineInterval.OneWeek,
        KlineInterval.OneMonth,
    ];

    // 大小寫敏感是刻意的:"1m" 是一分鐘、"1M" 是一個月,兩者只差在大小寫。
    // 用不分大小寫的比較器會讓一分鐘的訂閱靜默變成一個月,而且要等到很久之後才看得出不對。
    // The comparer is deliberately case-sensitive: "1m" is one minute while "1M" is one month, and the two differ
    // only by case. A case-insensitive comparer would silently turn a one-minute subscription into a monthly one.
    private static readonly FrozenDictionary<string, KlineInterval> Lookup =
        Supported.ToFrozenDictionary(interval => interval.ToExchangeString(), StringComparer.Ordinal);

    /// <summary>
    /// 全部支援的週期,不含 <see cref="KlineInterval.Unspecified"/>。
    /// Every supported interval, excluding <see cref="KlineInterval.Unspecified"/>.
    /// </summary>
    public static IReadOnlyList<KlineInterval> All { get; } = Supported;

    /// <summary>
    /// 嘗試把交易所週期字串解析成 <see cref="KlineInterval"/>。
    /// Tries to parse an exchange interval string into a <see cref="KlineInterval"/>.
    /// </summary>
    /// <param name="text">
    /// 週期字串,例如 <c>"15m"</c>。大小寫必須完全相符。
    /// The interval string such as <c>"15m"</c>. The comparison is case-sensitive.
    /// </param>
    /// <param name="interval">
    /// 成功時為對應的週期,失敗時為 <see cref="KlineInterval.Unspecified"/>。
    /// The parsed interval on success; <see cref="KlineInterval.Unspecified"/> on failure.
    /// </param>
    /// <returns>
    /// 解析成功時回傳 <see langword="true"/>。
    /// <see langword="true"/> when parsing succeeded.
    /// </returns>
    public static bool TryParse(string? text, out KlineInterval interval)
    {
        if (text is null)
        {
            interval = KlineInterval.Unspecified;
            return false;
        }

        return Lookup.TryGetValue(text, out interval);
    }

    /// <summary>
    /// 把交易所週期字串解析成 <see cref="KlineInterval"/>。
    /// Parses an exchange interval string into a <see cref="KlineInterval"/>.
    /// </summary>
    /// <param name="text">
    /// 週期字串,例如 <c>"4h"</c>。大小寫必須完全相符。
    /// The interval string such as <c>"4h"</c>. The comparison is case-sensitive.
    /// </param>
    /// <returns>
    /// 成功時為對應的週期;字串不支援時為代碼 <see cref="TradeErrorCodes.UnsupportedInterval"/> 的失敗結果。
    /// The interval on success, or a failure carrying <see cref="TradeErrorCodes.UnsupportedInterval"/> when the
    /// string is not supported.
    /// </returns>
    /// <remarks>
    /// 解析的是交易所回應或設定檔裡的字串,屬於執行期會失敗的正常情況,因此回傳 <see cref="Result{T}"/>
    /// 而不是擲出例外。
    /// The input comes from an exchange response or a configuration file, so failure is an ordinary runtime
    /// outcome and is expressed as a <see cref="Result{T}"/> rather than an exception.
    /// </remarks>
    public static Result<KlineInterval> Parse(string? text) =>
        TryParse(text, out var interval) ? interval : TradeErrors.UnsupportedInterval(text);
}
