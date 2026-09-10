namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// K 線週期。
/// The candlestick interval.
/// </summary>
/// <remarks>
/// 這個型別同時要能轉成交易所字串(例如 <c>"15m"</c>)與 <see cref="TimeSpan"/>:前者用於送出請求與訂閱串流,
/// 後者用於推算下一根 K 線何時收盤、回補多久的歷史資料。轉換方法見 <see cref="KlineIntervalExtensions"/>,
/// 反向解析見 <see cref="KlineIntervals"/>。
/// This type must convert both to the exchange string such as <c>"15m"</c> and to a <see cref="TimeSpan"/>: the
/// former for requests and stream subscriptions, the latter to work out when the next candle closes and how much
/// history to backfill. See <see cref="KlineIntervalExtensions"/> for conversions and <see cref="KlineIntervals"/>
/// for parsing.
/// </remarks>
public enum KlineInterval
{
    /// <summary>
    /// 未指定。屬於無效值,查詢與訂閱前必須被驗證擋下。
    /// Not specified. An invalid value that validation must reject before a query or subscription.
    /// </summary>
    Unspecified = 0,

    /// <summary>一分鐘(<c>1m</c>)。One minute (<c>1m</c>).</summary>
    OneMinute = 1,

    /// <summary>三分鐘(<c>3m</c>)。Three minutes (<c>3m</c>).</summary>
    ThreeMinutes = 2,

    /// <summary>五分鐘(<c>5m</c>)。Five minutes (<c>5m</c>).</summary>
    FiveMinutes = 3,

    /// <summary>十五分鐘(<c>15m</c>)。Fifteen minutes (<c>15m</c>).</summary>
    FifteenMinutes = 4,

    /// <summary>三十分鐘(<c>30m</c>)。Thirty minutes (<c>30m</c>).</summary>
    ThirtyMinutes = 5,

    /// <summary>一小時(<c>1h</c>)。One hour (<c>1h</c>).</summary>
    OneHour = 6,

    /// <summary>兩小時(<c>2h</c>)。Two hours (<c>2h</c>).</summary>
    TwoHours = 7,

    /// <summary>四小時(<c>4h</c>)。Four hours (<c>4h</c>).</summary>
    FourHours = 8,

    /// <summary>六小時(<c>6h</c>)。Six hours (<c>6h</c>).</summary>
    SixHours = 9,

    /// <summary>八小時(<c>8h</c>)。Eight hours (<c>8h</c>).</summary>
    EightHours = 10,

    /// <summary>十二小時(<c>12h</c>)。Twelve hours (<c>12h</c>).</summary>
    TwelveHours = 11,

    /// <summary>一日(<c>1d</c>)。One day (<c>1d</c>).</summary>
    OneDay = 12,

    /// <summary>三日(<c>3d</c>)。Three days (<c>3d</c>).</summary>
    ThreeDays = 13,

    /// <summary>一週(<c>1w</c>)。One week (<c>1w</c>).</summary>
    OneWeek = 14,

    /// <summary>
    /// 一個月(<c>1M</c>)。長度隨月份變動,沒有固定的 <see cref="TimeSpan"/>。
    /// One month (<c>1M</c>). Its length varies by month, so it has no fixed <see cref="TimeSpan"/>.
    /// </summary>
    OneMonth = 15,
}

/// <summary>
/// <see cref="KlineInterval"/> 的轉換與推算方法。
/// Conversion and arithmetic helpers for <see cref="KlineInterval"/>.
/// </summary>
public static class KlineIntervalExtensions
{
    /// <summary>
    /// 轉成交易所使用的週期字串。
    /// Converts the interval to the string an exchange expects.
    /// </summary>
    /// <param name="interval">要轉換的週期。The interval to convert.</param>
    /// <returns>
    /// 週期字串,例如 <c>"15m"</c>、<c>"4h"</c>、<c>"1M"</c>。
    /// The interval string, such as <c>"15m"</c>, <c>"4h"</c>, or <c>"1M"</c>.
    /// </returns>
    /// <remarks>
    /// 分鐘用小寫 <c>m</c>、月份用大寫 <c>M</c>,兩者只差在大小寫,轉換與解析都不可忽略大小寫。
    /// Minutes use a lower-case <c>m</c> and months an upper-case <c>M</c>. The two differ only by case, so neither
    /// conversion nor parsing may ever be case-insensitive.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="interval"/> 為 <see cref="KlineInterval.Unspecified"/> 或未定義的值時擲出。
    /// Thrown when <paramref name="interval"/> is <see cref="KlineInterval.Unspecified"/> or an undefined value.
    /// </exception>
    public static string ToExchangeString(this KlineInterval interval) => interval switch
    {
        KlineInterval.OneMinute => "1m",
        KlineInterval.ThreeMinutes => "3m",
        KlineInterval.FiveMinutes => "5m",
        KlineInterval.FifteenMinutes => "15m",
        KlineInterval.ThirtyMinutes => "30m",
        KlineInterval.OneHour => "1h",
        KlineInterval.TwoHours => "2h",
        KlineInterval.FourHours => "4h",
        KlineInterval.SixHours => "6h",
        KlineInterval.EightHours => "8h",
        KlineInterval.TwelveHours => "12h",
        KlineInterval.OneDay => "1d",
        KlineInterval.ThreeDays => "3d",
        KlineInterval.OneWeek => "1w",
        KlineInterval.OneMonth => "1M",
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "未定義的 K 線週期。Undefined kline interval."),
    };

    /// <summary>
    /// 這個週期的長度是否隨日曆變動(因此沒有固定的 <see cref="TimeSpan"/>)。
    /// Whether the interval's length depends on the calendar and therefore has no fixed <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="interval">要判斷的週期。The interval to inspect.</param>
    /// <returns>
    /// 僅 <see cref="KlineInterval.OneMonth"/> 回傳 <see langword="true"/>。
    /// Only <see cref="KlineInterval.OneMonth"/> returns <see langword="true"/>.
    /// </returns>
    public static bool IsCalendarBased(this KlineInterval interval) => interval == KlineInterval.OneMonth;

    /// <summary>
    /// 轉成固定長度的 <see cref="TimeSpan"/>。
    /// Converts the interval to its fixed-length <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="interval">要轉換的週期。The interval to convert.</param>
    /// <returns>該週期的長度。The length of one candle.</returns>
    /// <remarks>
    /// <see cref="KlineInterval.OneMonth"/> 沒有固定長度,呼叫前請先用 <see cref="IsCalendarBased"/> 判斷,
    /// 或改用 <see cref="TryGetDuration"/>。這裡不回傳「30 天」之類的近似值:排程與回補用了近似值之後,
    /// 誤差會累積成整根 K 線的偏移,而且症狀出現在很久以後。
    /// <see cref="KlineInterval.OneMonth"/> has no fixed length. Check <see cref="IsCalendarBased"/> first, or use
    /// <see cref="TryGetDuration"/>. No approximation such as "30 days" is returned: once scheduling or backfill
    /// uses an approximation the drift accumulates into a whole missing candle, and the symptom shows up much later.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="interval"/> 為 <see cref="KlineInterval.Unspecified"/> 或未定義的值時擲出。
    /// Thrown when <paramref name="interval"/> is <see cref="KlineInterval.Unspecified"/> or an undefined value.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="interval"/> 為日曆週期(<see cref="KlineInterval.OneMonth"/>)時擲出。
    /// Thrown when <paramref name="interval"/> is calendar based, that is <see cref="KlineInterval.OneMonth"/>.
    /// </exception>
    public static TimeSpan ToTimeSpan(this KlineInterval interval)
    {
        if (interval == KlineInterval.OneMonth)
        {
            throw new InvalidOperationException(
                "一個月的 K 線長度隨月份變動,沒有固定的 TimeSpan。The one-month interval has no fixed TimeSpan.");
        }

        return interval switch
        {
            KlineInterval.OneMinute => TimeSpan.FromMinutes(1),
            KlineInterval.ThreeMinutes => TimeSpan.FromMinutes(3),
            KlineInterval.FiveMinutes => TimeSpan.FromMinutes(5),
            KlineInterval.FifteenMinutes => TimeSpan.FromMinutes(15),
            KlineInterval.ThirtyMinutes => TimeSpan.FromMinutes(30),
            KlineInterval.OneHour => TimeSpan.FromHours(1),
            KlineInterval.TwoHours => TimeSpan.FromHours(2),
            KlineInterval.FourHours => TimeSpan.FromHours(4),
            KlineInterval.SixHours => TimeSpan.FromHours(6),
            KlineInterval.EightHours => TimeSpan.FromHours(8),
            KlineInterval.TwelveHours => TimeSpan.FromHours(12),
            KlineInterval.OneDay => TimeSpan.FromDays(1),
            KlineInterval.ThreeDays => TimeSpan.FromDays(3),
            KlineInterval.OneWeek => TimeSpan.FromDays(7),
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "未定義的 K 線週期。Undefined kline interval."),
        };
    }

    /// <summary>
    /// 嘗試取得固定長度,不會擲出例外。
    /// Tries to get the fixed length without throwing.
    /// </summary>
    /// <param name="interval">要轉換的週期。The interval to convert.</param>
    /// <param name="duration">
    /// 成功時為該週期的長度,失敗時為 <see cref="TimeSpan.Zero"/>。
    /// The length on success; <see cref="TimeSpan.Zero"/> on failure.
    /// </param>
    /// <returns>
    /// 該週期有固定長度時回傳 <see langword="true"/>。
    /// <see langword="true"/> when the interval has a fixed length.
    /// </returns>
    public static bool TryGetDuration(this KlineInterval interval, out TimeSpan duration)
    {
        if (interval == KlineInterval.Unspecified || interval.IsCalendarBased() || !Enum.IsDefined(interval))
        {
            duration = TimeSpan.Zero;
            return false;
        }

        duration = interval.ToTimeSpan();
        return true;
    }

    /// <summary>
    /// 推算下一根 K 線的開盤時間。
    /// Returns the open time of the next candle.
    /// </summary>
    /// <param name="interval">K 線週期。The interval.</param>
    /// <param name="openTime">
    /// 目前這根 K 線的開盤時間(UTC 語意)。
    /// The open time of the current candle, in UTC semantics.
    /// </param>
    /// <returns>下一根 K 線的開盤時間。The open time of the next candle.</returns>
    /// <remarks>
    /// 日曆週期用月份遞增而不是加固定天數,因此二月與三月都會落在正確的月初。
    /// The calendar interval advances by one month rather than a fixed number of days, so February and March both
    /// land on the correct start of month.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="interval"/> 為 <see cref="KlineInterval.Unspecified"/> 或未定義的值時擲出。
    /// Thrown when <paramref name="interval"/> is <see cref="KlineInterval.Unspecified"/> or an undefined value.
    /// </exception>
    public static DateTimeOffset GetNextOpenTime(this KlineInterval interval, DateTimeOffset openTime) =>
        interval.IsCalendarBased() ? openTime.AddMonths(1) : openTime + interval.ToTimeSpan();
}
