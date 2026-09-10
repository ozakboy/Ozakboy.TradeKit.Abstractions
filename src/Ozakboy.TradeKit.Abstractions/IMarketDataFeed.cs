using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 行情資料來源:歷史 K 線的查詢,以及 K 線與標記價的即時訂閱。
/// The market data source: historical kline queries plus live kline and mark price subscriptions.
/// </summary>
/// <remarks>
/// <para>
/// 即時訂閱用 <see cref="IAsyncEnumerable{T}"/> 而不是事件,理由有三個。
/// 其一,回測的模擬實作只要 <c>yield return</c> 一串歷史 K 線就完成了,不必自己維護訂閱者清單與執行緒;
/// 而事件模型的回測實作必須自己驅動事件、自己保證順序。
/// 其二,<c>await foreach</c> 天生是循序的,策略處理上一根 K 線的期間不會被下一根重入;事件則是誰觸發誰的
/// 執行緒在跑,策略得自己加鎖,而交易策略裡的競態是最難查的那種 bug。
/// 其三,取消與清理由 <see cref="CancellationToken"/> 一併處理,不像事件那樣需要記得取消訂閱 —— 忘記
/// <c>-=</c> 造成的記憶體洩漏在長時間執行的交易程式裡是常態。
/// Subscriptions are <see cref="IAsyncEnumerable{T}"/> rather than events for three reasons. First, a backtest
/// implementation is just a <c>yield return</c> over stored candles, with no subscriber list or threading of its
/// own, whereas an event-based one has to drive the events and guarantee their order itself. Second,
/// <c>await foreach</c> is sequential by construction, so a strategy is never re-entered with the next candle
/// while it is still handling the previous one; with events the publisher's thread runs the handler and the
/// strategy has to lock, and a race inside a trading strategy is the worst kind of bug to chase. Third,
/// cancellation and cleanup ride on the <see cref="CancellationToken"/> instead of an easily forgotten
/// <c>-=</c>, whose leak is routine in a long-running trading process.
/// </para>
/// <para>
/// 串流元素是 <see cref="Result{T}"/> 而不是裸資料:斷線重連、單筆訊息解析失敗都是即時串流的日常,
/// 消費端應該看得到失敗、決定要記錄後繼續還是中止,而不是被例外打斷 <c>await foreach</c>。
/// 實作若判定串流已無法恢復,應在推出最後一筆失敗元素之後結束列舉。
/// Stream elements are <see cref="Result{T}"/> rather than bare data: reconnects and unparsable messages are
/// everyday events on a live stream, and the consumer should see the failure and decide whether to log and carry
/// on or stop, instead of having an exception tear out of the <c>await foreach</c>. When an implementation decides
/// the stream is unrecoverable it should end the enumeration after yielding one final failure.
/// </para>
/// <para>
/// 實作若持有連線,可另外實作 <see cref="IAsyncDisposable"/>;本介面刻意不繼承它,免得純記憶體的
/// 回測實作被迫寫出空的釋放方法。
/// An implementation holding a connection may also implement <see cref="IAsyncDisposable"/>. This interface
/// deliberately does not inherit it, so that an in-memory backtest implementation is not forced into an empty
/// disposal method.
/// </para>
/// </remarks>
public interface IMarketDataFeed
{
    /// <summary>
    /// 查詢歷史 K 線。
    /// Queries historical klines.
    /// </summary>
    /// <param name="query">查詢條件。The query criteria.</param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 依開盤時間由舊到新排序的 K 線,或失敗原因。
    /// The candles ordered from oldest to newest by open time, or the reason it failed.
    /// </returns>
    Task<Result<IReadOnlyList<Kline>>> GetKlinesAsync(KlineQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 訂閱 K 線。
    /// Subscribes to klines.
    /// </summary>
    /// <param name="symbols">要訂閱的交易對。The symbols to subscribe to.</param>
    /// <param name="interval">K 線週期。The interval.</param>
    /// <param name="cancellationToken">
    /// 取消權杖。取消即代表結束訂閱。
    /// The cancellation token; cancelling it ends the subscription.
    /// </param>
    /// <returns>
    /// K 線串流。多個商品共用同一串流,以 <see cref="Kline.Symbol"/> 區分。
    /// The kline stream. Every symbol shares one stream and is told apart by <see cref="Kline.Symbol"/>.
    /// </returns>
    /// <remarks>
    /// 串流會同時推送未收盤與已收盤的 K 線,請以 <see cref="Kline.IsClosed"/> 過濾。
    /// The stream carries both in-progress and closed candles; filter on <see cref="Kline.IsClosed"/>.
    /// </remarks>
    IAsyncEnumerable<Result<Kline>> SubscribeKlinesAsync(
        IReadOnlyCollection<string> symbols,
        KlineInterval interval,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 訂閱標記價。
    /// Subscribes to mark prices.
    /// </summary>
    /// <param name="symbols">要訂閱的交易對。The symbols to subscribe to.</param>
    /// <param name="cancellationToken">
    /// 取消權杖。取消即代表結束訂閱。
    /// The cancellation token; cancelling it ends the subscription.
    /// </param>
    /// <returns>
    /// 標記價串流。多個商品共用同一串流,以 <see cref="MarkPriceUpdate.Symbol"/> 區分。
    /// The mark price stream, told apart by <see cref="MarkPriceUpdate.Symbol"/>.
    /// </returns>
    IAsyncEnumerable<Result<MarkPriceUpdate>> SubscribeMarkPricesAsync(
        IReadOnlyCollection<string> symbols,
        CancellationToken cancellationToken = default);
}
