namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 交易所中立的錯誤代碼常數,供 <see cref="Ozakboy.Core.Abstractions.Error.Code"/> 使用。
/// Exchange-neutral error codes for use as <see cref="Ozakboy.Core.Abstractions.Error.Code"/>.
/// </summary>
/// <remarks>
/// <para>
/// 實作套件的責任是把交易所自己的錯誤碼(幣安的 <c>-2011</c>、<c>-1021</c> 之類)對映到這一組常數,
/// 上層策略與回測引擎只認得這裡的代碼。少了這層對映,換交易所就要把所有 <c>if (code == -2011)</c> 重寫一次。
/// It is the implementation package's job to map the exchange's own codes — Binance's <c>-2011</c> or
/// <c>-1021</c>, for instance — onto these constants, so that strategies and the backtest engine only ever see
/// codes from this list. Without that mapping, switching exchanges means rewriting every
/// <c>if (code == -2011)</c> in the system.
/// </para>
/// <para>
/// 代碼一律小寫、以 <c>trade.</c> 開頭、單字之間用底線,與 <c>Ozakboy.Core.Abstractions</c> 的
/// <c>core.</c> 慣例一致。
/// Codes are lower-case, prefixed with <c>trade.</c>, and underscore-separated, matching the <c>core.</c>
/// convention in <c>Ozakboy.Core.Abstractions</c>.
/// </para>
/// </remarks>
public static class TradeErrorCodes
{
    /// <summary>
    /// 找不到這個交易對。
    /// The symbol does not exist on the exchange.
    /// </summary>
    public const string SymbolNotFound = "trade.symbol_not_found";

    /// <summary>
    /// 交易對存在但目前不可交易(下架、暫停、僅可平倉)。
    /// The symbol exists but is not currently tradable: delisted, halted, or reduce-only.
    /// </summary>
    public const string SymbolNotTradable = "trade.symbol_not_tradable";

    /// <summary>
    /// 交易規則本身不合法(步進值為零、上下限顛倒)。
    /// The trading rules themselves are invalid, such as a zero step size or inverted bounds.
    /// </summary>
    public const string InvalidSymbolRules = "trade.invalid_symbol_rules";

    /// <summary>
    /// 不支援的 K 線週期字串。
    /// The kline interval string is not supported.
    /// </summary>
    public const string UnsupportedInterval = "trade.unsupported_interval";

    /// <summary>
    /// 數量不合法(非正數、非有限值)。
    /// The quantity is invalid, for example zero or negative.
    /// </summary>
    public const string InvalidQuantity = "trade.invalid_quantity";

    /// <summary>
    /// 校正後的數量低於交易所允許的最小下單量。
    /// The normalised quantity is below the exchange minimum.
    /// </summary>
    public const string QuantityBelowMinimum = "trade.quantity_below_min";

    /// <summary>
    /// 數量超過交易所允許的單筆最大下單量。
    /// The quantity exceeds the exchange maximum for a single order.
    /// </summary>
    public const string QuantityAboveMaximum = "trade.quantity_above_max";

    /// <summary>
    /// 名目價值(價格 × 數量)低於交易所允許的最小值。
    /// The notional value, price times quantity, is below the exchange minimum.
    /// </summary>
    public const string NotionalBelowMinimum = "trade.notional_below_min";

    /// <summary>
    /// 價格不合法(非正數)。
    /// The price is invalid, for example zero or negative.
    /// </summary>
    public const string InvalidPrice = "trade.invalid_price";

    /// <summary>
    /// 價格超出交易所允許的範圍。
    /// The price falls outside the range the exchange accepts.
    /// </summary>
    public const string PriceOutOfRange = "trade.price_out_of_range";

    /// <summary>
    /// 下單請求的欄位組合不合法(限價單缺價格、條件單缺觸發價等)。
    /// The order request has an invalid combination of fields, such as a limit order with no price.
    /// </summary>
    public const string InvalidOrderRequest = "trade.invalid_order_request";

    /// <summary>
    /// 查詢條件不合法(時間區間顛倒、筆數上限非正數等)。
    /// The query criteria are invalid, such as an inverted time range or a non-positive limit.
    /// </summary>
    public const string InvalidQuery = "trade.invalid_query";

    /// <summary>
    /// 找不到這張委託。
    /// The order does not exist.
    /// </summary>
    public const string OrderNotFound = "trade.order_not_found";

    /// <summary>
    /// 用戶端訂單編號重複。冪等送單時代表這張單先前已經送出過。
    /// The client order id already exists; under idempotent submission this means the order was already sent.
    /// </summary>
    public const string DuplicateClientOrderId = "trade.duplicate_client_order_id";

    /// <summary>
    /// 委託被交易所拒絕。
    /// The exchange rejected the order.
    /// </summary>
    public const string OrderRejected = "trade.order_rejected";

    /// <summary>
    /// 委託已進入終態,無法撤銷。
    /// The order has already reached a terminal state and cannot be cancelled.
    /// </summary>
    public const string OrderNotCancelable = "trade.order_not_cancelable";

    /// <summary>
    /// 找不到這個商品的持倉。
    /// No position exists for the symbol.
    /// </summary>
    public const string PositionNotFound = "trade.position_not_found";

    /// <summary>
    /// 只減倉的委託會造成加倉,遭到拒絕。
    /// A reduce-only order was rejected because it would have increased the position.
    /// </summary>
    public const string ReduceOnlyRejected = "trade.reduce_only_rejected";

    /// <summary>
    /// 找不到這個資產的餘額。
    /// No balance exists for the asset.
    /// </summary>
    public const string BalanceNotFound = "trade.balance_not_found";

    /// <summary>
    /// 餘額不足。
    /// Insufficient balance.
    /// </summary>
    public const string InsufficientBalance = "trade.insufficient_balance";

    /// <summary>
    /// 保證金不足。
    /// Insufficient margin for the requested position.
    /// </summary>
    public const string InsufficientMargin = "trade.insufficient_margin";

    /// <summary>
    /// 槓桿倍數不被接受(超過該商品的分層上限)。
    /// The requested leverage is not accepted, typically above the symbol's tier limit.
    /// </summary>
    public const string LeverageNotAllowed = "trade.leverage_not_allowed";

    /// <summary>
    /// 保證金模式無法變更(通常是已有持倉或掛單)。
    /// The margin mode cannot be changed, usually because a position or open order exists.
    /// </summary>
    public const string MarginModeRejected = "trade.margin_mode_rejected";

    /// <summary>
    /// 被交易所限流。
    /// The exchange is rate limiting the caller.
    /// </summary>
    public const string RateLimited = "trade.rate_limited";

    /// <summary>
    /// 簽章驗證失敗。
    /// The request signature was rejected.
    /// </summary>
    public const string InvalidSignature = "trade.invalid_signature";

    /// <summary>
    /// 憑證不合法或已失效。
    /// The API credentials are invalid or revoked.
    /// </summary>
    public const string InvalidCredentials = "trade.invalid_credentials";

    /// <summary>
    /// 憑證正確但沒有這項操作的權限(例如未開啟合約交易權限)。
    /// The credentials are valid but lack permission for the operation.
    /// </summary>
    public const string PermissionDenied = "trade.permission_denied";

    /// <summary>
    /// 本機時間與交易所時間偏移過大,請求被拒。
    /// The local clock is too far from the exchange clock and the request was rejected.
    /// </summary>
    public const string TimestampOutOfSync = "trade.timestamp_out_of_sync";

    /// <summary>
    /// 交易所暫時無法服務(維護中、系統忙碌)。
    /// The exchange is temporarily unavailable, for example under maintenance.
    /// </summary>
    public const string ExchangeUnavailable = "trade.exchange_unavailable";

    /// <summary>
    /// 市場目前不接受委託。
    /// The market is not currently accepting orders.
    /// </summary>
    public const string MarketClosed = "trade.market_closed";

    /// <summary>
    /// 網路層失敗。
    /// A network-level failure.
    /// </summary>
    public const string NetworkFailure = "trade.network_failure";

    /// <summary>
    /// 請求逾時。
    /// The request timed out.
    /// </summary>
    public const string RequestTimeout = "trade.request_timeout";

    /// <summary>
    /// 即時資料串流中斷。
    /// The realtime data stream disconnected.
    /// </summary>
    public const string StreamDisconnected = "trade.stream_disconnected";

    /// <summary>
    /// 訂閱失敗。
    /// The subscription could not be established.
    /// </summary>
    public const string SubscriptionFailed = "trade.subscription_failed";

    /// <summary>
    /// 這個實作不支援該操作(例如回測用的模擬實作不支援變更保證金模式)。
    /// The implementation does not support the operation, such as a backtest stub that cannot change margin mode.
    /// </summary>
    public const string NotSupported = "trade.not_supported";

    /// <summary>
    /// 交易所回了無法對映到上述任何一項的錯誤。
    /// The exchange returned an error that maps to none of the above.
    /// </summary>
    public const string UnknownExchangeError = "trade.unknown_exchange_error";
}
