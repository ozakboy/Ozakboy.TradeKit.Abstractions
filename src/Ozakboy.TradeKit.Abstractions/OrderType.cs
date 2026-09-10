namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 委託類型。
/// The order type.
/// </summary>
/// <remarks>
/// 零值刻意保留為 <see cref="Unspecified"/>,避免忘記賦值時靜默送出市價單。各類型需要哪些欄位,
/// 由 <see cref="OrderRequest.Validate()"/> 統一把關。
/// The zero value is deliberately <see cref="Unspecified"/> so that a forgotten assignment cannot silently become
/// a market order. Which fields each type requires is enforced in one place by <see cref="OrderRequest.Validate()"/>.
/// </remarks>
public enum OrderType
{
    /// <summary>
    /// 未指定。屬於無效值,送單前必須被驗證擋下。
    /// Not specified. An invalid value that order validation must reject.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// 限價單:指定價格掛單,價格未到不成交。需要 <see cref="OrderRequest.Price"/>。
    /// A limit order resting at a given price. Requires <see cref="OrderRequest.Price"/>.
    /// </summary>
    Limit = 1,

    /// <summary>
    /// 市價單:以當下可成交的價格立即成交,不指定價格。
    /// A market order that fills immediately at whatever price is available.
    /// </summary>
    Market = 2,

    /// <summary>
    /// 停損市價單:觸發價到達後以市價成交,常用於停損出場。需要 <see cref="OrderRequest.StopPrice"/>。
    /// A stop order that becomes a market order once the trigger price is reached; the usual stop-loss exit.
    /// Requires <see cref="OrderRequest.StopPrice"/>.
    /// </summary>
    StopMarket = 3,

    /// <summary>
    /// 停損限價單:觸發價到達後掛出限價單。需要 <see cref="OrderRequest.StopPrice"/> 與 <see cref="OrderRequest.Price"/>。
    /// A stop order that places a limit order once the trigger price is reached. Requires both
    /// <see cref="OrderRequest.StopPrice"/> and <see cref="OrderRequest.Price"/>.
    /// </summary>
    StopLimit = 4,

    /// <summary>
    /// 停利市價單:觸發價到達後以市價成交。需要 <see cref="OrderRequest.StopPrice"/>。
    /// A take-profit order that becomes a market order once the trigger price is reached. Requires
    /// <see cref="OrderRequest.StopPrice"/>.
    /// </summary>
    TakeProfitMarket = 5,

    /// <summary>
    /// 停利限價單:觸發價到達後掛出限價單。需要 <see cref="OrderRequest.StopPrice"/> 與 <see cref="OrderRequest.Price"/>。
    /// A take-profit order that places a limit order once the trigger price is reached. Requires both
    /// <see cref="OrderRequest.StopPrice"/> and <see cref="OrderRequest.Price"/>.
    /// </summary>
    TakeProfitLimit = 6,

    /// <summary>
    /// 移動停損市價單:觸發價隨行情推進,回撤達設定比例才觸發。需要 <see cref="OrderRequest.CallbackRate"/>。
    /// A trailing stop whose trigger follows the market and fires after a given retracement. Requires
    /// <see cref="OrderRequest.CallbackRate"/>.
    /// </summary>
    TrailingStopMarket = 7,
}
