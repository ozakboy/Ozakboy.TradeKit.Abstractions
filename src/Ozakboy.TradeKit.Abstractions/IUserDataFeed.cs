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
}
