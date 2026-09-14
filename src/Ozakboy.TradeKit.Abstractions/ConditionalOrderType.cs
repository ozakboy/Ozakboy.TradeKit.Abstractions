namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 條件單的類型。
/// The type of a conditional order.
/// </summary>
/// <remarks>
/// <para>
/// 這是 <see cref="OrderType"/> 裡「需要觸發價才會生效」那幾項的獨立列舉。分開的理由是型別安全:
/// 條件單走的是另一組 API,把 <see cref="OrderType.Market"/> 送進條件單的路徑本來就沒有意義,
/// 而列舉分開之後那件事連編譯都過不了,不必等到執行期由驗證擋下。
/// This is a separate enum for the members of <see cref="OrderType"/> that only take effect once a trigger price
/// is reached. They are split for type safety: conditional orders travel over a different API, sending
/// <see cref="OrderType.Market"/> down that path is meaningless, and with separate enums it no longer compiles
/// instead of being caught by a run-time check.
/// </para>
/// <para>
/// 零值刻意保留為 <see cref="Unspecified"/>,理由同 <see cref="OrderType"/>:忘記賦值不可以靜默變成
/// 一種真的會送出去的委託。
/// The zero value is deliberately <see cref="Unspecified"/> for the same reason as on <see cref="OrderType"/>: a
/// forgotten assignment must not silently become an order that actually goes out.
/// </para>
/// </remarks>
public enum ConditionalOrderType
{
    /// <summary>
    /// 未指定。屬於無效值,送單前必須被驗證擋下。
    /// Not specified. An invalid value that order validation must reject.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// 停損市價單:觸發價到達後以市價成交。需要
    /// <see cref="ConditionalOrderRequest.TriggerPrice"/>。
    /// A stop order that becomes a market order once the trigger price is reached. Requires
    /// <see cref="ConditionalOrderRequest.TriggerPrice"/>.
    /// </summary>
    StopMarket = 1,

    /// <summary>
    /// 停損限價單:觸發價到達後掛出限價單。需要 <see cref="ConditionalOrderRequest.TriggerPrice"/>
    /// 與 <see cref="ConditionalOrderRequest.Price"/>。
    /// A stop order that places a limit order once the trigger price is reached. Requires both
    /// <see cref="ConditionalOrderRequest.TriggerPrice"/> and <see cref="ConditionalOrderRequest.Price"/>.
    /// </summary>
    /// <remarks>
    /// 當成停損用的時候要記得:觸發之後掛出去的是限價單,價格跳空穿過去就不會成交,部位留在原地。
    /// 真正要求「一定出場」的停損請用 <see cref="StopMarket"/>。
    /// Remember what this means as a stop-loss: what rests after the trigger is a limit order, and a gap straight
    /// through it simply does not fill, leaving the position open. Use <see cref="StopMarket"/> when the exit has
    /// to happen.
    /// </remarks>
    StopLimit = 2,

    /// <summary>
    /// 停利市價單:觸發價到達後以市價成交。需要 <see cref="ConditionalOrderRequest.TriggerPrice"/>。
    /// A take-profit order that becomes a market order once the trigger price is reached. Requires
    /// <see cref="ConditionalOrderRequest.TriggerPrice"/>.
    /// </summary>
    TakeProfitMarket = 3,

    /// <summary>
    /// 停利限價單:觸發價到達後掛出限價單。需要 <see cref="ConditionalOrderRequest.TriggerPrice"/>
    /// 與 <see cref="ConditionalOrderRequest.Price"/>。
    /// A take-profit order that places a limit order once the trigger price is reached. Requires both
    /// <see cref="ConditionalOrderRequest.TriggerPrice"/> and <see cref="ConditionalOrderRequest.Price"/>.
    /// </summary>
    TakeProfitLimit = 4,

    /// <summary>
    /// 移動停損市價單:觸發價隨行情推進,回撤達設定比例才觸發。需要
    /// <see cref="ConditionalOrderRequest.CallbackRate"/>,並以
    /// <see cref="ConditionalOrderRequest.ActivationPrice"/> 決定從哪個價位開始追蹤。
    /// A trailing stop whose trigger follows the market and fires after a given retracement. Requires
    /// <see cref="ConditionalOrderRequest.CallbackRate"/> and starts trailing from
    /// <see cref="ConditionalOrderRequest.ActivationPrice"/>.
    /// </summary>
    TrailingStopMarket = 5,
}

/// <summary>
/// <see cref="ConditionalOrderType"/> 的輔助方法。
/// Helper methods for <see cref="ConditionalOrderType"/>.
/// </summary>
public static class ConditionalOrderTypeExtensions
{
    /// <summary>
    /// 判斷這個類型觸發後掛出的是限價單(因而需要委託價與有效期限規則)。
    /// Determines whether the type places a limit order once triggered, and therefore needs a price and a time in
    /// force.
    /// </summary>
    /// <param name="type">要判斷的類型。The type to inspect.</param>
    /// <returns>
    /// 觸發後掛限價單時回傳 <see langword="true"/>。
    /// <see langword="true"/> when a limit order rests after the trigger.
    /// </returns>
    public static bool PlacesLimitOrder(this ConditionalOrderType type) => type switch
    {
        ConditionalOrderType.StopLimit or ConditionalOrderType.TakeProfitLimit => true,
        _ => false,
    };

    /// <summary>
    /// 取得對應的 <see cref="OrderType"/>,也就是這張條件單觸發之後實際送進簿子的委託類型。
    /// Returns the matching <see cref="OrderType"/>, that is, the order that actually reaches the book once this
    /// conditional order triggers.
    /// </summary>
    /// <param name="type">要轉換的類型。The type to convert.</param>
    /// <returns>
    /// 對應的委託類型;<see cref="ConditionalOrderType.Unspecified"/> 對映到
    /// <see cref="OrderType.Unspecified"/>。
    /// The matching order type; <see cref="ConditionalOrderType.Unspecified"/> maps to
    /// <see cref="OrderType.Unspecified"/>.
    /// </returns>
    /// <remarks>
    /// 用於把條件單併進既有的委託檢視,以及讓回測撮合器共用同一套成交邏輯。
    /// Used to fold conditional orders into an existing order view, and to let a backtest matcher reuse one set
    /// of fill logic.
    /// </remarks>
    public static OrderType ToOrderType(this ConditionalOrderType type) => type switch
    {
        ConditionalOrderType.StopMarket => OrderType.StopMarket,
        ConditionalOrderType.StopLimit => OrderType.StopLimit,
        ConditionalOrderType.TakeProfitMarket => OrderType.TakeProfitMarket,
        ConditionalOrderType.TakeProfitLimit => OrderType.TakeProfitLimit,
        ConditionalOrderType.TrailingStopMarket => OrderType.TrailingStopMarket,
        _ => OrderType.Unspecified,
    };
}
