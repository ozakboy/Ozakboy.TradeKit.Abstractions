namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// K 線週期在字串與 <see cref="TimeSpan"/> 之間雙向轉換的測試。
/// Tests for converting kline intervals to and from strings and <see cref="TimeSpan"/> values.
/// </summary>
[TestClass]
public sealed class KlineIntervalTests
{
    [TestMethod]
    [DataRow(KlineInterval.OneMinute, "1m")]
    [DataRow(KlineInterval.ThreeMinutes, "3m")]
    [DataRow(KlineInterval.FiveMinutes, "5m")]
    [DataRow(KlineInterval.FifteenMinutes, "15m")]
    [DataRow(KlineInterval.ThirtyMinutes, "30m")]
    [DataRow(KlineInterval.OneHour, "1h")]
    [DataRow(KlineInterval.TwoHours, "2h")]
    [DataRow(KlineInterval.FourHours, "4h")]
    [DataRow(KlineInterval.SixHours, "6h")]
    [DataRow(KlineInterval.EightHours, "8h")]
    [DataRow(KlineInterval.TwelveHours, "12h")]
    [DataRow(KlineInterval.OneDay, "1d")]
    [DataRow(KlineInterval.ThreeDays, "3d")]
    [DataRow(KlineInterval.OneWeek, "1w")]
    [DataRow(KlineInterval.OneMonth, "1M")]
    public void 轉成交易所字串(KlineInterval interval, string expected)
    {
        Assert.AreEqual(expected, interval.ToExchangeString());
    }

    [TestMethod]
    public void 每個週期都能字串來回轉換()
    {
        foreach (var interval in KlineIntervals.All)
        {
            var text = interval.ToExchangeString();
            var parsed = KlineIntervals.Parse(text);

            Assert.IsTrue(parsed.TryGetValue(out var roundTripped), parsed.Error?.ToString());
            Assert.AreEqual(interval, roundTripped, $"{text} 來回轉換不一致");
        }
    }

    [TestMethod]
    public void 分鐘與月份只差在大小寫且不可混淆()
    {
        Assert.IsTrue(KlineIntervals.TryParse("1m", out var minute));
        Assert.IsTrue(KlineIntervals.TryParse("1M", out var month));

        Assert.AreEqual(KlineInterval.OneMinute, minute);
        Assert.AreEqual(KlineInterval.OneMonth, month);
    }

    [TestMethod]
    [DataRow("1H")]
    [DataRow("15M")]
    [DataRow("1D")]
    public void 大小寫不符的字串不予解析(string text)
    {
        Assert.IsFalse(KlineIntervals.TryParse(text, out var interval));
        Assert.AreEqual(KlineInterval.Unspecified, interval);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("7m")]
    [DataRow("abc")]
    [DataRow(null)]
    public void 不支援的字串回報驗證失敗(string? text)
    {
        var result = KlineIntervals.Parse(text);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(TradeErrorCodes.UnsupportedInterval, result.Error!.Code);
        Assert.AreEqual(ErrorCategory.Validation, result.Error.Category);
    }

    [TestMethod]
    [DataRow(KlineInterval.OneMinute, 60)]
    [DataRow(KlineInterval.ThreeMinutes, 180)]
    [DataRow(KlineInterval.FifteenMinutes, 900)]
    [DataRow(KlineInterval.OneHour, 3600)]
    [DataRow(KlineInterval.FourHours, 14400)]
    [DataRow(KlineInterval.TwelveHours, 43200)]
    [DataRow(KlineInterval.OneDay, 86400)]
    [DataRow(KlineInterval.ThreeDays, 259200)]
    [DataRow(KlineInterval.OneWeek, 604800)]
    public void 轉成時間長度(KlineInterval interval, int expectedSeconds)
    {
        Assert.AreEqual(TimeSpan.FromSeconds(expectedSeconds), interval.ToTimeSpan());
        Assert.IsTrue(interval.TryGetDuration(out var duration));
        Assert.AreEqual(TimeSpan.FromSeconds(expectedSeconds), duration);
    }

    [TestMethod]
    public void 一個月沒有固定長度()
    {
        Assert.IsTrue(KlineInterval.OneMonth.IsCalendarBased());
        Assert.IsFalse(KlineInterval.OneMonth.TryGetDuration(out var duration));
        Assert.AreEqual(TimeSpan.Zero, duration);
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => KlineInterval.OneMonth.ToTimeSpan());
    }

    [TestMethod]
    public void 未指定的週期視為程式缺陷()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => KlineInterval.Unspecified.ToExchangeString());
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => KlineInterval.Unspecified.ToTimeSpan());
        Assert.IsFalse(KlineInterval.Unspecified.TryGetDuration(out _));
        Assert.IsFalse(KlineInterval.Unspecified.IsCalendarBased());
    }

    [TestMethod]
    public void 未定義的列舉值視為程式缺陷()
    {
        var undefined = (KlineInterval)999;

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => undefined.ToExchangeString());
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => undefined.ToTimeSpan());
        Assert.IsFalse(undefined.TryGetDuration(out _));
    }

    [TestMethod]
    public void 固定長度週期的下一根開盤時間是加上長度()
    {
        var openTime = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.AreEqual(
            openTime.AddMinutes(15),
            KlineInterval.FifteenMinutes.GetNextOpenTime(openTime));
    }

    [TestMethod]
    public void 月線的下一根開盤時間跟著日曆走()
    {
        var february = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var next = KlineInterval.OneMonth.GetNextOpenTime(february);

        Assert.AreEqual(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), next);
        Assert.AreEqual(28, (next - february).Days);
    }

    [TestMethod]
    public void 支援清單不含未指定值()
    {
        Assert.HasCount(15, KlineIntervals.All);
        Assert.IsFalse(KlineIntervals.All.Contains(KlineInterval.Unspecified));
    }
}
