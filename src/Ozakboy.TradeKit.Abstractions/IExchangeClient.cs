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
    /// <see cref="OrderRequest.ClientOrderId"/> 是冪等識別碼。送單逾時之後不可以直接重送:先用同一個
    /// <see cref="OrderRequest.ClientOrderId"/> 呼叫 <see cref="GetOrderAsync"/> 確認那張單到底進去了沒有,
    /// 否則會開出兩倍的部位。
    /// <see cref="OrderRequest.ClientOrderId"/> is the idempotency key. Never blindly resend after a timeout: look
    /// the order up with <see cref="GetOrderAsync"/> using the same id first, or the position ends up twice the
    /// intended size.
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
