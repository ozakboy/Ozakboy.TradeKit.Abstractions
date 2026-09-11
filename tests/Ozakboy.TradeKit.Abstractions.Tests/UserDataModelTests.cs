namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 使用者資料串流那三個新型別的契約:增量語意、預設值,以及「認不出來就落在 Unknown」。
/// The contracts of the three user-data stream types: delta semantics, defaults, and unrecognised values
/// landing on Unknown.
/// </summary>
[TestClass]
public sealed class UserDataModelTests
{
    private static readonly DateTimeOffset Moment = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 把列舉轉成整數,繞開 MSTEST0032:直接比較列舉常數會被判為「斷言條件恆為真」。
    /// Boxes an enum value through a call so MSTEST0032 does not flag the comparison as constantly true.
    /// </summary>
    private static int ToInt<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

    [TestMethod]
    public void 認不出來的原因一律落在Unknown而不是第一個有意義的值()
    {
        // 這兩個列舉的 0 必須是 Unknown。解析端讀到沒見過的代碼時會留在預設值上,
        // 若 0 是 Order,一筆看不懂的入金就會被靜靜算成交易損益。
        // 0 就是 default(TEnum),所以鎖住這個數值等於鎖住「未初始化與認不出來是同一件事」。
        Assert.AreEqual(0, ToInt(AccountUpdateReason.Unknown));
        Assert.AreEqual(0, ToInt(ResyncReason.Unknown));
    }

    [TestMethod]
    public void 帳戶變動預設為空的增量而不是空的快照()
    {
        var update = new AccountUpdate { Timestamp = Moment };

        Assert.IsEmpty(update.Balances);
        Assert.IsEmpty(update.Positions);
        Assert.AreEqual(AccountUpdateReason.Unknown, update.Reason);
        Assert.IsNull(update.RawReason);
        Assert.AreEqual(Moment, update.Timestamp);
    }

    [TestMethod]
    public void 帳戶變動只帶有動到的項目()
    {
        // 兩個部位的帳戶,這次只動到 BTCUSDT:ETHUSDT 不出現,而那不代表它被平掉了。
        var update = new AccountUpdate
        {
            Reason = AccountUpdateReason.FundingFee,
            RawReason = "FUNDING_FEE",
            Positions = [Position.Flat("BTCUSDT", Moment) with { Quantity = 0.5m }],
            Timestamp = Moment,
        };

        Assert.HasCount(1, update.Positions);
        Assert.AreEqual("BTCUSDT", update.Positions[0].Symbol);
        Assert.IsEmpty(update.Balances);
        Assert.AreEqual(AccountUpdateReason.FundingFee, update.Reason);
        Assert.AreEqual("FUNDING_FEE", update.RawReason);
    }

    [TestMethod]
    public void 原始原因代碼在列舉收不下時仍然看得到()
    {
        var update = new AccountUpdate
        {
            Reason = AccountUpdateReason.Unknown,
            RawReason = "OPTIONS_SETTLE_PROFIT",
            Timestamp = Moment,
        };

        Assert.AreEqual(AccountUpdateReason.Unknown, update.Reason);
        Assert.AreEqual("OPTIONS_SETTLE_PROFIT", update.RawReason);
    }

    [TestMethod]
    public void 保證金追繳只談被警告的部位()
    {
        var call = new MarginCall
        {
            CrossWalletBalance = 120.5m,
            Positions = [Position.Flat("BTCUSDT", Moment) with { Quantity = -1m, LiquidationPrice = 70_000m }],
            Timestamp = Moment,
        };

        Assert.HasCount(1, call.Positions);
        Assert.IsTrue(call.Positions[0].IsShort);
        Assert.AreEqual(120.5m, call.CrossWalletBalance);
    }

    [TestMethod]
    public void 保證金追繳的錢包餘額可以缺席()
    {
        var call = new MarginCall { Timestamp = Moment };

        Assert.IsNull(call.CrossWalletBalance);
        Assert.IsEmpty(call.Positions);
    }

    [TestMethod]
    public void 對帳訊號分開記錄從何時起不可信與何時發現()
    {
        // 斷線發生在 12:00,十秒後才重連成功並發出訊號。要補的資料是從 12:00 起算的,
        // 拿發出訊號的時間去查會漏掉那十秒。
        var signal = new ResyncRequired
        {
            Reason = ResyncReason.Reconnected,
            Detail = "串流中斷 10 秒",
            UntrustedSince = Moment,
            Timestamp = Moment.AddSeconds(10),
        };

        Assert.AreEqual(Moment, signal.UntrustedSince);
        Assert.AreEqual(Moment.AddSeconds(10), signal.Timestamp);
        Assert.IsGreaterThan(signal.UntrustedSince, signal.Timestamp);
        Assert.AreEqual(ResyncReason.Reconnected, signal.Reason);
    }

    [TestMethod]
    public void 對帳訊號預設為未知原因()
    {
        var signal = new ResyncRequired { Timestamp = Moment };

        Assert.AreEqual(ResyncReason.Unknown, signal.Reason);
        Assert.IsNull(signal.Detail);
    }

    [TestMethod]
    public void 三個型別都是實值相等()
    {
        var first = new AccountUpdate { Reason = AccountUpdateReason.Order, Timestamp = Moment };
        var second = new AccountUpdate { Reason = AccountUpdateReason.Order, Timestamp = Moment };

        Assert.AreEqual(first, second);
        Assert.AreNotEqual(first, first with { Reason = AccountUpdateReason.Liquidation });

        var call = new MarginCall { Timestamp = Moment };
        Assert.AreEqual(call, new MarginCall { Timestamp = Moment });

        var signal = new ResyncRequired { Reason = ResyncReason.EventGap, Timestamp = Moment };
        Assert.AreEqual(signal, new ResyncRequired { Reason = ResyncReason.EventGap, Timestamp = Moment });
    }
}
