using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 常用交易錯誤的建立方法,統一代碼、分類與訊息格式。
/// Factory methods for the common trading failures, keeping code, category, and message format consistent.
/// </summary>
/// <remarks>
/// 數值一律以 <see cref="Precision.ToPlainString(decimal)"/> 序列化:錯誤訊息裡出現 <c>1E-05</c> 這種科學記號
/// 會讓事後看 log 的人完全看不出真正的數量。
/// Numbers are always serialised with <see cref="Precision.ToPlainString(decimal)"/>: exponent notation such as
/// <c>1E-05</c> in a log line hides the real quantity from whoever reads it later.
/// </remarks>
public static class TradeErrors
{
    /// <summary>
    /// 建立「找不到交易對」的錯誤。
    /// Creates a symbol-not-found failure.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="symbol"/> 為 <see langword="null"/> 時擲出。
    /// Thrown when <paramref name="symbol"/> is <see langword="null"/>.
    /// </exception>
    public static Error SymbolNotFound(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        return Error.NotFound(
            TradeErrorCodes.SymbolNotFound,
            $"找不到交易對 {symbol}。Symbol {symbol} was not found.");
    }

    /// <summary>
    /// 建立「交易對目前不可交易」的錯誤。
    /// Creates a symbol-not-tradable failure.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="symbol"/> 為 <see langword="null"/> 時擲出。
    /// Thrown when <paramref name="symbol"/> is <see langword="null"/>.
    /// </exception>
    public static Error SymbolNotTradable(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        return Error.Conflict(
            TradeErrorCodes.SymbolNotTradable,
            $"交易對 {symbol} 目前不可交易。Symbol {symbol} is not currently tradable.");
    }

    /// <summary>
    /// 建立「數量不合法」的錯誤。
    /// Creates an invalid-quantity failure.
    /// </summary>
    /// <param name="quantity">呼叫端提供的數量。The quantity supplied by the caller.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    public static Error InvalidQuantity(decimal quantity) =>
        Error.Validation(
            TradeErrorCodes.InvalidQuantity,
            $"數量必須大於零,收到 {Precision.ToPlainString(quantity)}。Quantity must be greater than zero but was {Precision.ToPlainString(quantity)}.");

    /// <summary>
    /// 建立「數量低於最小下單量」的錯誤。
    /// Creates a quantity-below-minimum failure.
    /// </summary>
    /// <param name="quantity">校正後的數量。The normalised quantity.</param>
    /// <param name="minimum">交易所允許的最小下單量。The exchange minimum.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    public static Error QuantityBelowMinimum(decimal quantity, decimal minimum) =>
        Error.Validation(
            TradeErrorCodes.QuantityBelowMinimum,
            $"校正後數量 {Precision.ToPlainString(quantity)} 低於最小下單量 {Precision.ToPlainString(minimum)}。Normalised quantity {Precision.ToPlainString(quantity)} is below the minimum {Precision.ToPlainString(minimum)}.");

    /// <summary>
    /// 建立「數量超過單筆上限」的錯誤。
    /// Creates a quantity-above-maximum failure.
    /// </summary>
    /// <param name="quantity">校正後的數量。The normalised quantity.</param>
    /// <param name="maximum">交易所允許的單筆最大下單量。The exchange maximum for a single order.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    public static Error QuantityAboveMaximum(decimal quantity, decimal maximum) =>
        Error.Validation(
            TradeErrorCodes.QuantityAboveMaximum,
            $"數量 {Precision.ToPlainString(quantity)} 超過單筆上限 {Precision.ToPlainString(maximum)},請拆單。Quantity {Precision.ToPlainString(quantity)} exceeds the per-order maximum {Precision.ToPlainString(maximum)}; split the order.");

    /// <summary>
    /// 建立「名目價值低於下限」的錯誤。
    /// Creates a notional-below-minimum failure.
    /// </summary>
    /// <param name="notional">名目價值(價格 × 數量)。The notional value, price times quantity.</param>
    /// <param name="minimum">交易所允許的最小名目價值。The exchange minimum notional.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    public static Error NotionalBelowMinimum(decimal notional, decimal minimum) =>
        Error.Validation(
            TradeErrorCodes.NotionalBelowMinimum,
            $"名目價值 {Precision.ToPlainString(notional)} 低於最小名目價值 {Precision.ToPlainString(minimum)}。Notional {Precision.ToPlainString(notional)} is below the minimum {Precision.ToPlainString(minimum)}.");

    /// <summary>
    /// 建立「價格不合法」的錯誤。
    /// Creates an invalid-price failure.
    /// </summary>
    /// <param name="price">呼叫端提供的價格。The price supplied by the caller.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    public static Error InvalidPrice(decimal price) =>
        Error.Validation(
            TradeErrorCodes.InvalidPrice,
            $"價格必須大於零,收到 {Precision.ToPlainString(price)}。Price must be greater than zero but was {Precision.ToPlainString(price)}.");

    /// <summary>
    /// 建立「價格超出允許範圍」的錯誤。
    /// Creates a price-out-of-range failure.
    /// </summary>
    /// <param name="price">校正後的價格。The normalised price.</param>
    /// <param name="minimum">允許的最低價。The lowest accepted price.</param>
    /// <param name="maximum">允許的最高價。The highest accepted price.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    public static Error PriceOutOfRange(decimal price, decimal minimum, decimal maximum) =>
        Error.Validation(
            TradeErrorCodes.PriceOutOfRange,
            $"價格 {Precision.ToPlainString(price)} 超出允許範圍 {Precision.ToPlainString(minimum)} ~ {Precision.ToPlainString(maximum)}。Price {Precision.ToPlainString(price)} is outside the accepted range {Precision.ToPlainString(minimum)} to {Precision.ToPlainString(maximum)}.");

    /// <summary>
    /// 建立「下單請求欄位組合不合法」的錯誤。
    /// Creates an invalid-order-request failure.
    /// </summary>
    /// <param name="reason">
    /// 說明哪一個欄位組合不合法,請一併寫成中英雙語。
    /// A bilingual explanation of which field combination is invalid.
    /// </param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="reason"/> 為空白時擲出。
    /// Thrown when <paramref name="reason"/> is blank.
    /// </exception>
    public static Error InvalidOrderRequest(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return Error.Validation(TradeErrorCodes.InvalidOrderRequest, reason);
    }

    /// <summary>
    /// 建立「查詢條件不合法」的錯誤。
    /// Creates an invalid-query failure.
    /// </summary>
    /// <param name="reason">
    /// 說明哪一個條件不合法,請一併寫成中英雙語。
    /// A bilingual explanation of which criterion is invalid.
    /// </param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="reason"/> 為空白時擲出。
    /// Thrown when <paramref name="reason"/> is blank.
    /// </exception>
    public static Error InvalidQuery(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return Error.Validation(TradeErrorCodes.InvalidQuery, reason);
    }

    /// <summary>
    /// 建立「交易規則本身不合法」的錯誤。
    /// Creates an invalid-symbol-rules failure.
    /// </summary>
    /// <param name="reason">
    /// 說明哪一條規則不合法,請一併寫成中英雙語。
    /// A bilingual explanation of which rule is invalid.
    /// </param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="reason"/> 為空白時擲出。
    /// Thrown when <paramref name="reason"/> is blank.
    /// </exception>
    public static Error InvalidSymbolRules(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return Error.Validation(TradeErrorCodes.InvalidSymbolRules, reason);
    }

    /// <summary>
    /// 建立「找不到委託」的錯誤。
    /// Creates an order-not-found failure.
    /// </summary>
    /// <param name="identifier">用來查詢的訂單識別碼。The identifier used for the lookup.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    public static Error OrderNotFound(OrderIdentifier identifier) =>
        Error.NotFound(
            TradeErrorCodes.OrderNotFound,
            $"找不到委託 {identifier}。Order {identifier} was not found.");

    /// <summary>
    /// 建立「找不到條件單」的錯誤。
    /// Creates a conditional-order-not-found failure.
    /// </summary>
    /// <param name="identifier">用來查詢的條件單識別碼。The identifier used for the lookup.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    public static Error ConditionalOrderNotFound(ConditionalOrderIdentifier identifier) =>
        Error.NotFound(
            TradeErrorCodes.ConditionalOrderNotFound,
            $"找不到條件單 {identifier}。Conditional order {identifier} was not found.");

    /// <summary>
    /// 建立「這個委託類型必須走條件單路徑」的錯誤。
    /// Creates a conditional-order-path-required failure.
    /// </summary>
    /// <param name="orderType">呼叫端送出的委託類型。The order type the caller submitted.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    /// <remarks>
    /// 訊息刻意點名該改呼叫哪一個方法:這個錯誤幾乎都出現在「程式碼是條件單搬家以前寫的」這個情境,
    /// 收到的人需要的是下一步怎麼做,不是再一句「不被接受」。
    /// The message deliberately names the method to call instead. This failure almost always means the code
    /// predates the exchange moving conditional orders, and whoever reads it needs the next step rather than
    /// another way of saying "not accepted".
    /// </remarks>
    public static Error ConditionalOrderPathRequired(OrderType orderType) =>
        Error.Validation(
            TradeErrorCodes.ConditionalOrderPathRequired,
            $"{orderType} 屬於條件單,必須改用 IExchangeClient.PlaceConditionalOrderAsync 送出。{orderType} is a conditional order and must be submitted through IExchangeClient.PlaceConditionalOrderAsync.");

    /// <summary>
    /// 建立「找不到持倉」的錯誤。
    /// Creates a position-not-found failure.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="symbol"/> 為 <see langword="null"/> 時擲出。
    /// Thrown when <paramref name="symbol"/> is <see langword="null"/>.
    /// </exception>
    public static Error PositionNotFound(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        return Error.NotFound(
            TradeErrorCodes.PositionNotFound,
            $"找不到 {symbol} 的持倉。No position was found for {symbol}.");
    }

    /// <summary>
    /// 建立「找不到資產餘額」的錯誤。
    /// Creates a balance-not-found failure.
    /// </summary>
    /// <param name="asset">資產代碼。The asset.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="asset"/> 為 <see langword="null"/> 時擲出。
    /// Thrown when <paramref name="asset"/> is <see langword="null"/>.
    /// </exception>
    public static Error BalanceNotFound(string asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        return Error.NotFound(
            TradeErrorCodes.BalanceNotFound,
            $"找不到 {asset} 的餘額。No balance was found for {asset}.");
    }

    /// <summary>
    /// 建立「不支援的 K 線週期字串」的錯誤。
    /// Creates an unsupported-interval failure.
    /// </summary>
    /// <param name="interval">收到的週期字串。The interval string received.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    public static Error UnsupportedInterval(string? interval) =>
        Error.Validation(
            TradeErrorCodes.UnsupportedInterval,
            $"不支援的 K 線週期「{interval}」。Unsupported kline interval \"{interval}\".");

    /// <summary>
    /// 建立「這個實作不支援該操作」的錯誤。
    /// Creates a not-supported failure.
    /// </summary>
    /// <param name="operation">操作名稱。The operation name.</param>
    /// <returns>對應的錯誤。The corresponding error.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="operation"/> 為空白時擲出。
    /// Thrown when <paramref name="operation"/> is blank.
    /// </exception>
    public static Error NotSupported(string operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        return Error.Conflict(
            TradeErrorCodes.NotSupported,
            $"這個實作不支援 {operation}。This implementation does not support {operation}.");
    }
}
