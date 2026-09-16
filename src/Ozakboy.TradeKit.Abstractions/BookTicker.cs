namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 盤口最佳買賣價與其掛單量,也就是買一與賣一。
/// The best bid and ask with the quantity resting at each: the top of the book.
/// </summary>
/// <remarks>
/// <para>
/// 這是純 maker 掛單的定價依據。想要不吃單、只掛單(Post-Only / GTX)的策略,必須把買單掛在
/// <see cref="BidPrice"/> 或更低、賣單掛在 <see cref="AskPrice"/> 或更高;拿最新成交價或
/// <see cref="MarkPriceUpdate.MarkPrice"/> 代替會定出一個「會立即成交」的價格,交易所直接拒單,
/// 而那張單從頭到尾沒有進過簿子。
/// This is what a pure-maker order is priced from. A strategy that must not take liquidity — Post-Only, GTX —
/// places its buys at <see cref="BidPrice"/> or below and its sells at <see cref="AskPrice"/> or above.
/// Substituting the last traded price or <see cref="MarkPriceUpdate.MarkPrice"/> produces a price that would
/// match immediately, and the exchange rejects the order outright: it never reaches the book at all.
/// </para>
/// <para>
/// <b>盤口會過期,而過期的盤口看起來和新鮮的一模一樣。</b>用一秒前的買一去掛單,價格可能已經穿到對手方,
/// 結果同樣是被拒單(或更糟:掛成吃單方,付的是 taker 費率)。因此 <see cref="ReceivedAt"/> 是必填的,
/// 呼叫端下單前應該自己判斷這筆盤口還能不能用。
/// <b>A book goes stale, and a stale one looks exactly like a fresh one.</b> Quoting from a bid that is a second
/// old can land on the wrong side of the spread, earning the same rejection — or worse, a fill as the taker at
/// the taker fee. <see cref="ReceivedAt"/> is therefore required, and callers are expected to judge for
/// themselves whether a book is still usable before sending an order.
/// </para>
/// <para>
/// 四個價量欄位都是 <see cref="decimal"/>,而且都是必填 —— 盤口缺了任何一邊就不是盤口。實作若從交易所
/// 收到缺欄位的訊息,應該讓整筆判讀失敗,不可以填 0:0 會被讀成「賣一是零元」或「買一無人掛單」,
/// 前者算出來的掛單價是零,後者讓策略以為市場沒有深度。
/// All four price and quantity members are <see cref="decimal"/> and all are required: a book missing one side is
/// not a book. An implementation receiving a message without one of them should fail the whole read rather than
/// substitute zero, which would read as "the ask is zero" or "nobody is bidding" — the first prices an order at
/// nothing and the second tells a strategy the market has no depth.
/// </para>
/// </remarks>
public sealed record BookTicker
{
    /// <summary>
    /// 交易對代碼。
    /// The symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 買一價,也就是目前最高的買單價格。
    /// The best bid: the highest price anyone is currently bidding.
    /// </summary>
    public required decimal BidPrice { get; init; }

    /// <summary>
    /// 買一的掛單量,以基礎幣計。
    /// The quantity resting at the best bid, in base asset units.
    /// </summary>
    /// <remarks>
    /// 掛單量決定了「掛在這個價位要排多久的隊」。要掛的量遠大於這個數字時,成交會分批發生,
    /// 中間那段時間裡價格可能已經走掉。
    /// The size at the top tells how long the queue at that price is. An order much larger than this figure fills
    /// in pieces, and the price can walk away in between.
    /// </remarks>
    public required decimal BidQuantity { get; init; }

    /// <summary>
    /// 賣一價,也就是目前最低的賣單價格。
    /// The best ask: the lowest price anyone is currently offering.
    /// </summary>
    public required decimal AskPrice { get; init; }

    /// <summary>
    /// 賣一的掛單量,以基礎幣計。
    /// The quantity resting at the best ask, in base asset units.
    /// </summary>
    public required decimal AskQuantity { get; init; }

    /// <summary>
    /// 交易所標記這筆盤口的時間(UTC 語意);交易所未提供時為 <see langword="null"/>。
    /// The exchange's own timestamp for this book, in UTC semantics, or <see langword="null"/> when the exchange
    /// does not provide one.
    /// </summary>
    /// <remarks>
    /// 有些交易所的即時推送帶時間戳,REST 查詢卻不帶(或反過來)。缺值一律是 <see langword="null"/>,
    /// 不可以拿 <see cref="ReceivedAt"/> 填進來 —— 那會讓「交易所說的時間」與「我收到的時間」變成同一個數字,
    /// 兩者相減出來的延遲就永遠是零,而量測延遲正是這個欄位存在的理由。
    /// Some exchanges timestamp the live push but not the REST response, or the other way round. A missing value
    /// stays <see langword="null"/> and is never filled from <see cref="ReceivedAt"/>: doing so would collapse
    /// "when the exchange says this was true" and "when it arrived here" into one number, making the difference
    /// between them permanently zero — and measuring that difference is why this member exists.
    /// </remarks>
    public DateTimeOffset? ExchangeTime { get; init; }

    /// <summary>
    /// 本地收到這筆盤口的時間(UTC 語意)。
    /// When this book arrived locally, in UTC semantics.
    /// </summary>
    /// <remarks>
    /// 刻意必填而不給預設值。預設的 <see cref="DateTimeOffset"/> 是 0001-01-01,拿它算出來的「盤口年齡」
    /// 是兩千年,新鮮度檢查會把每一筆盤口都判成過期 —— 策略於是一張單都掛不出去,而畫面上沒有任何錯誤。
    /// Required on purpose, with no default. A default <see cref="DateTimeOffset"/> is 0001-01-01, which makes
    /// every book two thousand years old: a freshness check then rejects all of them and the strategy quietly
    /// stops placing orders, with nothing anywhere reporting an error.
    /// </remarks>
    public required DateTimeOffset ReceivedAt { get; init; }

    /// <summary>
    /// 買賣價差,等於賣一價減買一價。
    /// The spread, equal to the ask price minus the bid price.
    /// </summary>
    /// <remarks>
    /// 正常情況下是正數。負數代表盤口交叉(買一高於賣一),那不是可以下單的市場狀態,而是資料有問題:
    /// 兩邊來自不同時刻、或訊息亂序。看到負數應該丟掉這筆盤口重取,不要拿它定價。
    /// Positive in normal conditions. A negative value means a crossed book, which is not a market to trade into
    /// but a sign that the data is wrong: the two sides came from different instants, or messages arrived out of
    /// order. Discard such a book and fetch another rather than pricing from it.
    /// </remarks>
    public decimal Spread => AskPrice - BidPrice;

    /// <summary>
    /// 買賣價的中點。
    /// The midpoint between the bid and the ask.
    /// </summary>
    /// <remarks>
    /// 中點是估值用的參考價,<b>不是可以成交的價格</b>:掛在中點的買單高於買一,對純 maker 的策略來說
    /// 就是一張會立即成交的單。要掛單請用 <see cref="BidPrice"/> 與 <see cref="AskPrice"/> 本身。
    /// The midpoint is a reference for valuation and <b>not a tradable price</b>: a buy at the midpoint sits above
    /// the best bid, which for a pure-maker strategy is an order that matches on arrival. Quote from
    /// <see cref="BidPrice"/> and <see cref="AskPrice"/> themselves.
    /// </remarks>
    public decimal MidPrice => (BidPrice + AskPrice) / 2m;
}
