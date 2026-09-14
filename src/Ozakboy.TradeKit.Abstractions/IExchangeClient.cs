using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 合約交易所的操作介面:查帳戶、查持倉、下單、撤單、查單,以及交易規則(繼承自
/// <see cref="IExchangeInfoProvider"/>)。
/// The operations of a derivatives exchange: account, positions, order placement, cancellation, order lookup, and
/// the trading rules inherited from <see cref="IExchangeInfoProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// 這個介面的驗收標準是「一個純記憶體的模擬撮合器要能完整實作它」。回測引擎注入的就是那個模擬實作,
/// 因此這裡不會出現任何暗示必須有網路連線、必須有 WebSocket、必須有 API 金鑰的設計。
/// The acceptance criterion for this interface is that a purely in-memory matching engine can implement all of it.
/// The backtest engine injects exactly such an implementation, so nothing here presumes a network connection, a
/// WebSocket, or an API key.
/// </para>
/// <para>
/// 所有方法都回傳 <see cref="Result{T}"/>:被限流、被拒單、找不到訂單都是交易系統每天都會遇到的正常情況,
/// 不是例外。真正的程式缺陷(參數為 <see langword="null"/>)才擲出例外。
/// Every method returns a <see cref="Result{T}"/>: rate limiting, rejections, and missing orders are routine in a
/// trading system rather than exceptional. Only genuine defects, such as a <see langword="null"/> argument, throw.
/// </para>
/// </remarks>
public interface IExchangeClient : IExchangeInfoProvider
{
    /// <summary>
    /// 取得帳戶快照(餘額與持倉一次取回)。
    /// Gets an account snapshot with balances and positions in one call.
    /// </summary>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>帳戶快照,或失敗原因。The snapshot, or the reason it failed.</returns>
    Task<Result<AccountSnapshot>> GetAccountSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得目前全部持倉。
    /// Gets every current position.
    /// </summary>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>持倉清單,或失敗原因。The positions, or the reason it failed.</returns>
    Task<Result<IReadOnlyList<Position>>> GetPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得單一商品的持倉。
    /// Gets the position on one symbol.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 持倉;空手時回傳數量為零的持倉,而不是失敗。
    /// The position; a flat position with zero quantity is returned rather than a failure when there is none.
    /// </returns>
    Task<Result<Position>> GetPositionAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// 送出一張委託。
    /// Places one order.
    /// </summary>
    /// <param name="request">
    /// 下單請求。呼叫端應先以 <see cref="OrderRequest.NormalizeFor"/> 校正價量。
    /// The order request. Callers should normalise it with <see cref="OrderRequest.NormalizeFor"/> first.
    /// </param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 交易所接受後的委託狀態,或失敗原因。
    /// The order as the exchange accepted it, or the reason it failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <see cref="OrderRequest.ClientOrderId"/> 是冪等識別碼。送單逾時之後不可以直接重送:先用同一個
    /// <see cref="OrderRequest.ClientOrderId"/> 呼叫 <see cref="GetOrderAsync"/> 確認那張單到底進去了沒有,
    /// 否則會開出兩倍的部位。
    /// <see cref="OrderRequest.ClientOrderId"/> is the idempotency key. Never blindly resend after a timeout: look
    /// the order up with <see cref="GetOrderAsync"/> using the same id first, or the position ends up twice the
    /// intended size.
    /// </para>
    /// <para>
    /// <b>這個方法只收非條件單。</b>停損、停利與移動停損請走 <see cref="PlaceConditionalOrderAsync"/>
    /// —— 那幾個類型送到這裡會被交易所拒絕,實作也會以代碼
    /// <see cref="TradeErrorCodes.ConditionalOrderPathRequired"/> 先擋下來。
    /// <b>This method takes non-conditional orders only.</b> Stops, take-profits, and trailing stops go through
    /// <see cref="PlaceConditionalOrderAsync"/>: the exchange rejects those types here, and implementations
    /// short-circuit them with <see cref="TradeErrorCodes.ConditionalOrderPathRequired"/>.
    /// </para>
    /// </remarks>
    Task<Result<Order>> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤銷一張委託。
    /// Cancels one order.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <param name="identifier">訂單識別碼。The order identifier.</param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 撤銷後的委託狀態;找不到訂單時為代碼 <see cref="TradeErrorCodes.OrderNotFound"/> 的失敗。
    /// The order after cancellation, or a failure carrying <see cref="TradeErrorCodes.OrderNotFound"/>.
    /// </returns>
    Task<Result<Order>> CancelOrderAsync(
        string symbol,
        OrderIdentifier identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤銷某商品的全部掛單。
    /// Cancels every open order on one symbol.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>全部撤銷成功時為成功,否則為失敗原因。Success when all were cancelled; otherwise the failure.</returns>
    /// <remarks>
    /// 沒有掛單時視為成功,不算失敗 —— 緊急出場流程會無條件先撤單再平倉,那條路徑上「本來就沒單」
    /// 是正常情況。
    /// Having nothing to cancel counts as success: an emergency exit cancels before closing unconditionally, and
    /// "there was nothing there" is the normal case on that path.
    /// </remarks>
    Task<Result> CancelAllOrdersAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢單一委託。
    /// Looks up one order.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <param name="identifier">訂單識別碼。The order identifier.</param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 委託狀態;找不到時為代碼 <see cref="TradeErrorCodes.OrderNotFound"/> 的失敗。
    /// The order, or a failure carrying <see cref="TradeErrorCodes.OrderNotFound"/>.
    /// </returns>
    Task<Result<Order>> GetOrderAsync(
        string symbol,
        OrderIdentifier identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢尚未結束的委託。
    /// Lists the orders that are still live.
    /// </summary>
    /// <param name="symbol">
    /// 要查詢的交易對;<see langword="null"/> 代表全部商品。
    /// The symbol to query, or <see langword="null"/> for every symbol.
    /// </param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>掛單清單,或失敗原因。The open orders, or the reason it failed.</returns>
    Task<Result<IReadOnlyList<Order>>> GetOpenOrdersAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 送出一張條件單(停損、停利或移動停損)。
    /// Places one conditional order: a stop, a take-profit, or a trailing stop.
    /// </summary>
    /// <param name="request">
    /// 條件單請求。呼叫端應先以 <see cref="ConditionalOrderRequest.NormalizeFor"/> 校正價量。
    /// The conditional order request. Callers should normalise it with
    /// <see cref="ConditionalOrderRequest.NormalizeFor"/> first.
    /// </param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 交易所接受後的條件單狀態,或失敗原因。
    /// The conditional order as the exchange accepted it, or the reason it failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// 條件單與一般委託分成兩個方法,是因為交易所把它們放在兩條獨立的路徑上:編號自成一套、
    /// 撤單端點不同、狀態機也不一樣。<see cref="PlaceOrderAsync"/> 收到條件單類型會被拒絕。
    /// Conditional orders get their own method because exchanges keep them on a separate path: their own
    /// numbering, a different cancellation endpoint, and a different state machine.
    /// <see cref="PlaceOrderAsync"/> rejects conditional types.
    /// </para>
    /// <para>
    /// <see cref="ConditionalOrderRequest.ClientConditionalOrderId"/> 是冪等識別碼,規矩與一般委託相同:
    /// 送單逾時之後先用 <see cref="GetConditionalOrderAsync"/> 查,不可以直接重送。停損重複掛尤其危險 ——
    /// 部位被其中一張平掉之後,另一張會反手開出一個沒人要的反向部位。
    /// <see cref="ConditionalOrderRequest.ClientConditionalOrderId"/> is the idempotency key and the rule is the
    /// same as for ordinary orders: after a timeout, look it up with <see cref="GetConditionalOrderAsync"/>
    /// rather than resending. A duplicated stop is the worst case — once one of them closes the position, the
    /// other opens an unwanted one in the opposite direction.
    /// </para>
    /// <para>
    /// <b>回測撮合器的語意</b>:條件單在 <see cref="ConditionalOrderStatus.New"/> 期間不佔簿上的位置、
    /// 不吃保證金,撮合器每根 K 線只需判斷觸發價有沒有被穿越。用哪個價格判斷由
    /// <see cref="ConditionalOrderRequest.TriggerPriceType"/> 決定:
    /// <see cref="TriggerPriceType.MarkPrice"/> 看標記價,<see cref="TriggerPriceType.LastPrice"/> 看成交價;
    /// 回測資料若只有成交價,<b>必須</b>把這件事講明,不可以拿成交價冒充標記價 ——
    /// 那會讓實盤看標記價的停損在回測裡被 K 線影線掃出場,回測結果比實盤悲觀,而且悲觀得看不出來。
    /// 觸發之後才生出一張 <see cref="Order"/>(市價類型立即撮合,限價類型掛進簿子),
    /// 其編號填回 <see cref="ConditionalOrder.TriggeredOrderId"/>。
    /// <b>Semantics in a backtest matcher</b>: while a conditional order is
    /// <see cref="ConditionalOrderStatus.New"/> it occupies no place on the book and consumes no margin, and the
    /// matcher need only check each candle for a crossing of the trigger price. Which price to compare is decided
    /// by <see cref="ConditionalOrderRequest.TriggerPriceType"/>:
    /// <see cref="TriggerPriceType.MarkPrice"/> watches the mark price and
    /// <see cref="TriggerPriceType.LastPrice"/> the traded price. A backtest whose data holds only traded prices
    /// <b>must</b> say so rather than passing them off as mark prices: a stop that watches the mark price live
    /// then gets taken out by candle wicks in the backtest, making the results pessimistic in a way nothing in
    /// them reveals. Only after the trigger does an <see cref="Order"/> come into existence — matched immediately
    /// for the market types, rested on the book for the limit ones — and its id goes into
    /// <see cref="ConditionalOrder.TriggeredOrderId"/>.
    /// </para>
    /// </remarks>
    Task<Result<ConditionalOrder>> PlaceConditionalOrderAsync(
        ConditionalOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤銷一張條件單。
    /// Cancels one conditional order.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <param name="identifier">條件單識別碼。The conditional order identifier.</param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 撤銷後的條件單狀態;找不到時為代碼 <see cref="TradeErrorCodes.ConditionalOrderNotFound"/> 的失敗。
    /// The conditional order after cancellation, or a failure carrying
    /// <see cref="TradeErrorCodes.ConditionalOrderNotFound"/>.
    /// </returns>
    Task<Result<ConditionalOrder>> CancelConditionalOrderAsync(
        string symbol,
        ConditionalOrderIdentifier identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢單一條件單。
    /// Looks up one conditional order.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <param name="identifier">條件單識別碼。The conditional order identifier.</param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 條件單狀態;找不到時為代碼 <see cref="TradeErrorCodes.ConditionalOrderNotFound"/> 的失敗。
    /// The conditional order, or a failure carrying <see cref="TradeErrorCodes.ConditionalOrderNotFound"/>.
    /// </returns>
    /// <remarks>
    /// 交易所通常只保留有限期間內的條件單紀錄,太舊的會查不到 —— 這時回的是「找不到」,
    /// 而不是「已撤銷」。要判斷停損現在還在不在,用 <see cref="GetOpenConditionalOrdersAsync"/>。
    /// Exchanges typically keep conditional order history for a limited window, and anything older comes back as
    /// not found rather than as cancelled. To find out whether a stop is still in place, use
    /// <see cref="GetOpenConditionalOrdersAsync"/>.
    /// </remarks>
    Task<Result<ConditionalOrder>> GetConditionalOrderAsync(
        string symbol,
        ConditionalOrderIdentifier identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢尚未結束的條件單。
    /// Lists the conditional orders that are still live.
    /// </summary>
    /// <param name="symbol">
    /// 要查詢的交易對;<see langword="null"/> 代表全部商品。
    /// The symbol to query, or <see langword="null"/> for every symbol.
    /// </param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>條件單清單,或失敗原因。The open conditional orders, or the reason it failed.</returns>
    /// <remarks>
    /// 這是對帳時確認「每個部位都還有停損保護著」的來源。<see cref="GetOpenOrdersAsync"/> 查不到條件單,
    /// 只看那一個會得到「沒有任何掛單」的結論,而停損其實好端端地掛在另一條路徑上 —— 或者根本不在。
    /// This is where reconciliation confirms that every position still has a stop behind it.
    /// <see cref="GetOpenOrdersAsync"/> does not see conditional orders, so relying on it alone concludes "no
    /// resting orders" while the stops sit safely on the other path — or are genuinely missing.
    /// </remarks>
    Task<Result<IReadOnlyList<ConditionalOrder>>> GetOpenConditionalOrdersAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤銷某商品的全部條件單。
    /// Cancels every open conditional order on one symbol.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>全部撤銷成功時為成功,否則為失敗原因。Success when all were cancelled; otherwise the failure.</returns>
    /// <remarks>
    /// 與 <see cref="CancelAllOrdersAsync"/> 一樣,沒有條件單可撤時視為成功。
    /// 也與它一樣<b>不是</b>彼此的替代品:緊急出場要兩個都呼叫,只撤一般委託會留下停損單,
    /// 平倉之後那張停損就成了反向開倉的引信。
    /// As with <see cref="CancelAllOrdersAsync"/>, having nothing to cancel counts as success. Also as with it,
    /// the two are <b>not</b> substitutes: an emergency exit calls both, because cancelling only the ordinary
    /// orders leaves the stops behind, and after the position closes such a stop becomes the fuse for an
    /// inverted one.
    /// </remarks>
    Task<Result> CancelAllConditionalOrdersAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// 設定某商品的槓桿倍數。
    /// Sets the leverage on one symbol.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <param name="leverage">槓桿倍數,必須至少為 1。The leverage; at least 1.</param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 設定成功時為成功;不被接受時為代碼 <see cref="TradeErrorCodes.LeverageNotAllowed"/> 的失敗。
    /// Success when applied, or a failure carrying <see cref="TradeErrorCodes.LeverageNotAllowed"/>.
    /// </returns>
    Task<Result> SetLeverageAsync(string symbol, int leverage, CancellationToken cancellationToken = default);

    /// <summary>
    /// 設定某商品的保證金模式。
    /// Sets the margin mode on one symbol.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <param name="marginMode">保證金模式。The margin mode.</param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 設定成功時為成功;有持倉或掛單而無法變更時為代碼
    /// <see cref="TradeErrorCodes.MarginModeRejected"/> 的失敗。
    /// Success when applied, or a failure carrying <see cref="TradeErrorCodes.MarginModeRejected"/> when an open
    /// position or order blocks the change.
    /// </returns>
    /// <remarks>
    /// 模式本來就已經是目標值時視為成功。交易所對「沒有變更」通常回錯誤,實作要把它吃掉,
    /// 否則每次啟動都會出現一則假的錯誤告警。
    /// Already being in the requested mode counts as success. Exchanges usually return an error for "no change",
    /// and the implementation must swallow it or every start-up raises a false alert.
    /// </remarks>
    Task<Result> SetMarginModeAsync(
        string symbol,
        MarginMode marginMode,
        CancellationToken cancellationToken = default);
}
