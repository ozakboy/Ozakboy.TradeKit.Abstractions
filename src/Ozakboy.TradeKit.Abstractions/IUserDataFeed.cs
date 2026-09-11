using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 帳戶私有資料的即時來源:委託狀態變化與成交明細。
/// The live source of private account data: order state changes and fills.
/// </summary>
/// <remarks>
/// <para>
/// 沒有這個介面就只能輪詢查單,而輪詢會漏掉「掛單成交後又立刻被停損平掉」這種在兩次輪詢之間發生完的
/// 狀態變化,部位追蹤會與交易所對不起來。
/// Without this interface the only option is polling, and polling misses state changes that begin and end between
/// two polls — a resting order filling and then being stopped out — leaving position tracking out of step with the
/// exchange.
/// </para>
/// <para>
/// 與 <see cref="IMarketDataFeed"/> 同樣採用 <see cref="IAsyncEnumerable{T}"/> 加 <see cref="Result{T}"/>,
/// 理由見該介面說明。回測的模擬實作把撮合產生的成交 <c>yield return</c> 出來即可。
/// It uses <see cref="IAsyncEnumerable{T}"/> with <see cref="Result{T}"/> for the reasons given on
/// <see cref="IMarketDataFeed"/>. A backtest implementation simply yields the fills its matching engine produces.
/// </para>
/// <para>
/// <b>每一條訂閱都只送出「從訂閱那一刻起」發生的事件,先前的不補送。</b>實作通常共用同一條連線,
/// 分別送給各個訂閱者,所以晚訂閱的那一條會錯過已經流過去的事件。要補回來只有一個辦法:
/// 全量對帳 —— 而那本來就是啟動時必做的事,不該由串流兼差。
/// <b>Every subscription delivers only what happens from the moment it subscribes; nothing earlier is
/// replayed.</b> Implementations typically share one connection and fan it out, so a subscription opened late
/// misses whatever has already gone past. The only way to recover it is a full reconciliation — which is
/// required at start-up regardless, and is not a job the stream should be asked to do as a sideline.
/// </para>
/// <para>
/// <see cref="SubscribeResyncSignalsAsync"/> 是這個設計的另一半:串流斷過之後,它會告訴你本地狀態
/// 從哪一刻起不可信。沒有訂閱它,上面那個「不補送」的語意就變成一個無聲的資料遺失。
/// <see cref="SubscribeResyncSignalsAsync"/> is the other half of that design: after an interruption it says
/// from when local state stopped being trustworthy. Without subscribing to it, "nothing is replayed" becomes
/// silent data loss.
/// </para>
/// </remarks>
public interface IUserDataFeed
{
    /// <summary>
    /// 訂閱委託狀態變化。
    /// Subscribes to order state changes.
    /// </summary>
    /// <param name="cancellationToken">
    /// 取消權杖。取消即代表結束訂閱。
    /// The cancellation token; cancelling it ends the subscription.
    /// </param>
    /// <returns>委託更新串流。The stream of order updates.</returns>
    IAsyncEnumerable<Result<Order>> SubscribeOrderUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 訂閱成交明細。
    /// Subscribes to fills.
    /// </summary>
    /// <param name="cancellationToken">
    /// 取消權杖。取消即代表結束訂閱。
    /// The cancellation token; cancelling it ends the subscription.
    /// </param>
    /// <returns>成交串流。The stream of fills.</returns>
    IAsyncEnumerable<Result<Trade>> SubscribeTradeUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 訂閱帳戶餘額與部位的變動。
    /// Subscribes to changes in account balances and positions.
    /// </summary>
    /// <remarks>
    /// 送出的是<b>增量</b>,見 <see cref="AccountUpdate"/>。部位會因為成交以外的理由改變 ——
    /// 資金費結算、保證金轉帳、強平 —— 只看委託與成交推不出正確的權益。
    /// The stream carries <b>deltas</b>; see <see cref="AccountUpdate"/>. Positions move for reasons other than
    /// fills — funding settlements, margin transfers, liquidations — and equity derived from orders and fills
    /// alone is wrong.
    /// </remarks>
    /// <param name="cancellationToken">
    /// 取消權杖。取消即代表結束訂閱。
    /// The cancellation token; cancelling it ends the subscription.
    /// </param>
    /// <returns>帳戶變動串流。The stream of account changes.</returns>
    IAsyncEnumerable<Result<AccountUpdate>> SubscribeAccountUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 訂閱保證金追繳警告。
    /// Subscribes to margin calls.
    /// </summary>
    /// <remarks>
    /// 這是交易所主動送出的最後一次提醒,下一步就是強平,因此這條串流的事件<b>不可以</b>被當成
    /// 可丟棄的通知處理。
    /// This is the last warning the exchange volunteers before liquidating, so events on this stream <b>must
    /// not</b> be treated as a notification that may be dropped.
    /// </remarks>
    /// <param name="cancellationToken">
    /// 取消權杖。取消即代表結束訂閱。
    /// The cancellation token; cancelling it ends the subscription.
    /// </param>
    /// <returns>保證金追繳串流。The stream of margin calls.</returns>
    IAsyncEnumerable<Result<MarginCall>> SubscribeMarginCallsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 訂閱「本地狀態已經不可信,請全量對帳」的訊號。
    /// Subscribes to the signal that local state must be reconciled in full.
    /// </summary>
    /// <remarks>
    /// 串流斷過就會漏事件,而漏掉的部分交易所不補送。這條串流是唯一會告訴你「剛剛漏了」的管道;
    /// 不訂閱它,本地部位會與交易所安靜地分岔,而帳面上的數字依然合理。詳見 <see cref="ResyncRequired"/>。
    /// An interrupted stream loses events and the exchange does not replay them. This is the only channel that
    /// says a loss just happened; without it, local positions drift from the exchange's in silence while the
    /// numbers on the books stay plausible. See <see cref="ResyncRequired"/>.
    /// </remarks>
    /// <param name="cancellationToken">
    /// 取消權杖。取消即代表結束訂閱。
    /// The cancellation token; cancelling it ends the subscription.
    /// </param>
    /// <returns>對帳訊號串流。The stream of reconciliation signals.</returns>
    IAsyncEnumerable<Result<ResyncRequired>> SubscribeResyncSignalsAsync(CancellationToken cancellationToken = default);
}
