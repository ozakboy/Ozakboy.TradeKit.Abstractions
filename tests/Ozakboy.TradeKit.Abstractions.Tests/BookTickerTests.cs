namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 盤口型別的衍生欄位與語意測試。
/// Tests for the derived members and semantics of the book ticker.
/// </summary>
[TestClass]
public sealed class BookTickerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static BookTicker NewBook() => new()
    {
        Symbol = "BTCUSDT",
        BidPrice = 50_000.1m,
        BidQuantity = 1.5m,
        AskPrice = 50_000.3m,
        AskQuantity = 2.5m,
        ExchangeTime = Now,
        ReceivedAt = Now.AddMilliseconds(12),
    };

    [TestMethod]
    public void 價差等於賣一減買一()
    {
        Assert.AreEqual(0.2m, NewBook().Spread);
    }

    [TestMethod]
    public void 中價是買一與賣一的中點()
    {
        Assert.AreEqual(50_000.2m, NewBook().MidPrice);
    }

    [TestMethod]
    public void 盤口交叉時價差為負()
    {
        // 買一高於賣一不是可以下單的市場,而是資料出了問題(兩邊來自不同時刻或訊息亂序)。
        // 價差算出負數,消費端才有機會把這筆丟掉重取。
        // A bid above the ask is not a tradable market but broken data, and the negative spread is what gives a
        // consumer the chance to discard the book instead of pricing from it.
        var crossed = NewBook() with { BidPrice = 50_000.5m };

        Assert.IsTrue(crossed.Spread < 0m);
    }

    [TestMethod]
    public void 價量一律以十進位保存不會出現二進位誤差()
    {
        // 0.1 在二進位浮點數裡沒有精確表示,decimal 才有。這一條守的是「盤口價不得改成 double」。
        // Ten-cent quantities have no exact binary representation; decimal does. This guards the rule that book
        // prices must never become doubles.
        var book = NewBook() with { BidPrice = 0.1m, AskPrice = 0.3m };

        Assert.AreEqual(0.2m, book.Spread);
        Assert.AreEqual(0.2m, book.MidPrice);
    }

    [TestMethod]
    public void 交易所未提供時間戳時為空值而不是收到時間()
    {
        // 缺值不可以拿 ReceivedAt 填,否則兩者相減出來的延遲永遠是零。
        // A missing exchange timestamp must not be filled from ReceivedAt, or the latency between them is
        // permanently zero.
        var book = NewBook() with { ExchangeTime = null };

        Assert.IsNull(book.ExchangeTime);
        Assert.AreEqual(Now.AddMilliseconds(12), book.ReceivedAt);
    }

    [TestMethod]
    public void 收到時間不同的兩筆盤口不相等()
    {
        // 盤口是 record,值相等要把時間也算進去:同樣的價量在不同時刻是兩筆不同的觀測,
        // 其中一筆可能已經過期。
        // The book is a record and its value equality has to include the timestamps: the same prices observed at
        // two instants are two observations, and one of them may already be stale.
        var first = NewBook();
        var second = first with { ReceivedAt = first.ReceivedAt.AddMilliseconds(1) };

        Assert.AreNotEqual(first, second);
        Assert.AreEqual(first, first with { });
    }
}
