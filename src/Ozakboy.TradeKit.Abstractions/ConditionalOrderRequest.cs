using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 建立一張條件單所需的全部參數。
/// Everything needed to create one conditional order.
/// </summary>
/// <remarks>
/// <para>
/// 條件單與一般委託是兩條分開的路徑:它在觸發之前不佔簿上的位置,交易所也常把它放在另一組端點底下管理,
/// 編號、狀態、撤單方式都自成一套。因此這裡是獨立的請求型別,而不是 <see cref="OrderRequest"/> 多幾個欄位
/// —— 欄位混在一起,「市價單帶觸發價」這種組合就得靠執行期驗證擋,而不是根本寫不出來。
/// A conditional order travels a different path from an ordinary one: it occupies no place on the book until it
/// triggers, and exchanges commonly manage it under a separate set of endpoints with its own ids, statuses, and
/// cancellation semantics. This is therefore its own request type rather than a few more fields on
/// <see cref="OrderRequest"/> — with the fields merged, a combination like "a market order carrying a trigger
/// price" has to be caught by a run-time check instead of being impossible to write.
/// </para>
/// <para>
/// 與 <see cref="OrderRequest"/> 一樣,<see cref="Validate()"/> 在本地檢查欄位組合,
/// <see cref="NormalizeFor"/> 套用交易規則,兩者都不需要網路。
/// As with <see cref="OrderRequest"/>, <see cref="Validate()"/> checks the field combination locally and
/// <see cref="NormalizeFor"/> applies the symbol's trading rules; neither needs a network.
/// </para>
/// <para>
/// <see cref="ClientConditionalOrderId"/> 是冪等識別碼,強烈建議每張條件單都自己給。停損單尤其不能盲目重送:
/// 送單逾時之後重掛一張,原本那張若其實已經進去了,部位平掉之後留下來的那一張會反手開倉。
/// <see cref="ClientConditionalOrderId"/> is the idempotency key and should be set on every conditional order.
/// Stops in particular must never be resent blindly: if the timed-out submission did reach the exchange, the
/// duplicate survives the position closing and opens a new one in the opposite direction.
/// </para>
/// </remarks>
public sealed record ConditionalOrderRequest
{
    /// <summary>
    /// 交易對代碼,必須與 <see cref="SymbolInfo.Name"/> 完全相同。
    /// The symbol, which must match <see cref="SymbolInfo.Name"/> exactly.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 買賣方向。
    /// The order side.
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// 條件單類型。
    /// The conditional order type.
    /// </summary>
    public required ConditionalOrderType ConditionalOrderType { get; init; }

    /// <summary>
    /// 觸發價。移動停損以外的類型都必須指定。
    /// The trigger price. Required by every type except the trailing stop.
    /// </summary>
    /// <remarks>
    /// 由 <see cref="TriggerPriceType"/> 決定拿哪一種價格與它比較。
    /// Which price it is compared against is decided by <see cref="TriggerPriceType"/>.
    /// </remarks>
    public decimal? TriggerPrice { get; init; }

    /// <summary>
    /// 觸發價要與哪一種價格比較,預設標記價。
    /// Which price the trigger price is compared against; defaults to the mark price.
    /// </summary>
    public TriggerPriceType TriggerPriceType { get; init; } = TriggerPriceType.MarkPrice;

    /// <summary>
    /// 委託數量,以基礎幣計價。<see cref="ClosePosition"/> 為 <see langword="true"/> 時必須為 0。
    /// The order quantity in base asset units. Must be zero when <see cref="ClosePosition"/> is
    /// <see langword="true"/>.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// 全部平倉:觸發時由交易所以當下的部位規模成交,不指定數量。
    /// Close position: on trigger the exchange sizes the order from the current position instead of an explicit
    /// quantity.
    /// </summary>
    /// <remarks>
    /// 這是停損單最穩妥的用法:部位在這張停損掛著的期間被加碼或減碼,它依然平得乾淨,不會留下尾巴。
    /// 只有市價類型支援 —— 觸發後掛限價單卻要求「全部平掉」,在限價沒成交時等於沒有平倉。
    /// This is the safest way to hold a stop: if the position is scaled up or down while the stop rests, it still
    /// closes cleanly with nothing left over. Only the market-style types support it — asking a resting limit
    /// order to close everything means nothing is closed when it does not fill.
    /// </remarks>
    public bool ClosePosition { get; init; }

    /// <summary>
    /// 只減倉:這張單只能縮小既有部位,不能加倉或反向開倉。
    /// Reduce only: the order may only shrink an existing position, never add to it or flip it.
    /// </summary>
    public bool ReduceOnly { get; init; }

    /// <summary>
    /// 觸發後掛出的限價,僅 <see cref="ConditionalOrderType.StopLimit"/> 與
    /// <see cref="ConditionalOrderType.TakeProfitLimit"/> 使用。
    /// The limit price used after the trigger, only by <see cref="ConditionalOrderType.StopLimit"/> and
    /// <see cref="ConditionalOrderType.TakeProfitLimit"/>.
    /// </summary>
    public decimal? Price { get; init; }

    /// <summary>
    /// 觸發後掛出的委託的有效期限規則,預設 <see cref="TimeInForce.GoodTilCanceled"/>。僅限價類型使用。
    /// The time in force of the order placed after the trigger, defaulting to
    /// <see cref="TimeInForce.GoodTilCanceled"/>. Used only by the limit-style types.
    /// </summary>
    public TimeInForce TimeInForce { get; init; } = TimeInForce.GoodTilCanceled;

    /// <summary>
    /// 移動停損的回撤比例,單位為百分比(例如 <c>1.5</c> 代表 1.5%)。僅
    /// <see cref="ConditionalOrderType.TrailingStopMarket"/> 使用。
    /// The trailing stop callback rate as a percentage, where <c>1.5</c> means 1.5%. Used only by
    /// <see cref="ConditionalOrderType.TrailingStopMarket"/>.
    /// </summary>
    public decimal? CallbackRate { get; init; }

    /// <summary>
    /// 移動停損的啟動價:行情到達這個價位之後才開始追蹤。僅
    /// <see cref="ConditionalOrderType.TrailingStopMarket"/> 使用,留白代表立即以當下價格開始追蹤。
    /// The activation price of a trailing stop: it only starts following the market once this price is reached.
    /// Used only by <see cref="ConditionalOrderType.TrailingStopMarket"/>; leaving it unset starts trailing from
    /// the current price immediately.
    /// </summary>
    public decimal? ActivationPrice { get; init; }

    /// <summary>
    /// 持倉方向,預設 <see cref="PositionSide.Both"/>(單向模式)。
    /// The position side, defaulting to <see cref="PositionSide.Both"/> for one-way mode.
    /// </summary>
    public PositionSide PositionSide { get; init; } = PositionSide.Both;

    /// <summary>
    /// 用戶端條件單編號(冪等識別碼)。留白時由實作套件產生。
    /// The client conditional order id, which acts as the idempotency key. Implementations generate one when it is
    /// left unset.
    /// </summary>
    /// <remarks>
    /// 各交易所對這個字串的字元集與長度各有規定,格式由實作套件把關,這一層只要求它不是空白。
    /// Exchanges each impose their own character set and length limits on this string. The implementation package
    /// enforces the format; this layer only requires that it is not blank.
    /// </remarks>
    public string? ClientConditionalOrderId { get; init; }

    /// <summary>
    /// 檢查欄位組合是否合法(不套用交易規則)。
    /// Checks that the field combination is valid, without applying any symbol trading rules.
    /// </summary>
    /// <returns>
    /// 合法時為成功,否則為代碼 <see cref="TradeErrorCodes.InvalidOrderRequest"/> 的失敗。
    /// Success when valid; otherwise a failure carrying <see cref="TradeErrorCodes.InvalidOrderRequest"/>.
    /// </returns>
    public Result Validate()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            return TradeErrors.InvalidOrderRequest("交易對代碼不可為空白。The symbol must not be blank.");
        }

        if (Side == OrderSide.Unspecified)
        {
            return TradeErrors.InvalidOrderRequest("必須指定買賣方向。The order side must be specified.");
        }

        if (ConditionalOrderType == ConditionalOrderType.Unspecified)
        {
            return TradeErrors.InvalidOrderRequest("必須指定條件單類型。The conditional order type must be specified.");
        }

        if (ClientConditionalOrderId is not null && string.IsNullOrWhiteSpace(ClientConditionalOrderId))
        {
            return TradeErrors.InvalidOrderRequest("用戶端條件單編號不可為空白字串,不需要時請留為 null。The client conditional order id must not be a blank string; leave it null when unused.");
        }

        if (ClosePosition && ReduceOnly)
        {
            return TradeErrors.InvalidOrderRequest("ClosePosition 與 ReduceOnly 不可同時指定。ClosePosition and ReduceOnly are mutually exclusive.");
        }

        if (ClosePosition)
        {
            if (Quantity != 0m)
            {
                return TradeErrors.InvalidOrderRequest("ClosePosition 為 true 時不可指定數量。Quantity must be zero when ClosePosition is true.");
            }

            if (ConditionalOrderType.PlacesLimitOrder())
            {
                return TradeErrors.InvalidOrderRequest($"{ConditionalOrderType} 觸發後掛的是限價單,不支援 ClosePosition:限價沒成交就等於沒有平倉。{ConditionalOrderType} rests a limit order after the trigger and cannot close a position: nothing is closed when that limit does not fill.");
            }
        }
        else if (Quantity <= 0m)
        {
            return TradeErrors.InvalidOrderRequest("數量必須大於零。Quantity must be greater than zero.");
        }

        return ValidateTypeSpecificFields();
    }

    /// <summary>
    /// 依交易對的交易規則校正價量,回傳可直接送出的請求。
    /// Applies the symbol's trading rules to the prices and quantity, returning a request that is ready to send.
    /// </summary>
    /// <param name="symbol">交易對的交易規則。The symbol's trading rules.</param>
    /// <param name="rounding">
    /// 價格的對齊方向,預設取最接近的跳動點。
    /// The price rounding direction; defaults to the nearest tick.
    /// </param>
    /// <returns>
    /// 校正後的新請求(原請求不變);欄位組合不合法、商品不可交易或校正後根本下不了單時為失敗。
    /// A new normalised request, leaving the original untouched. Fails when the field combination is invalid, the
    /// symbol is not tradable, or the normalised order could not be traded at all.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <see cref="TriggerPrice"/>、<see cref="Price"/> 與 <see cref="ActivationPrice"/> 都會對齊到跳動點:
    /// 觸發價沒對齊一樣會被拒單,而停損被拒單是最不該發生的那一種拒單。
    /// <see cref="TriggerPrice"/>, <see cref="Price"/>, and <see cref="ActivationPrice"/> are all aligned to the
    /// tick size: an unaligned trigger price is rejected just like any other, and a rejected stop is the one
    /// rejection that must not happen.
    /// </para>
    /// <para>
    /// 最小名目價值依序以 <see cref="Price"/>、<see cref="TriggerPrice"/>、<see cref="ActivationPrice"/> 檢查。
    /// <see cref="ClosePosition"/> 的請求沒有數量可校正,直接略過。
    /// The minimum notional is checked against <see cref="Price"/>, then <see cref="TriggerPrice"/>, then
    /// <see cref="ActivationPrice"/>. A <see cref="ClosePosition"/> request has no quantity to normalise and skips
    /// that step.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="symbol"/> 為 <see langword="null"/> 時擲出。
    /// Thrown when <paramref name="symbol"/> is <see langword="null"/>.
    /// </exception>
    public Result<ConditionalOrderRequest> NormalizeFor(
        SymbolInfo symbol,
        PriceRounding rounding = PriceRounding.Nearest)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var validation = Validate();

        if (validation.IsFailure)
        {
            return validation.Error!;
        }

        if (!string.Equals(symbol.Name, Symbol, StringComparison.Ordinal))
        {
            return TradeErrors.InvalidOrderRequest(
                $"交易規則屬於 {symbol.Name},與請求的 {Symbol} 不符。The rules belong to {symbol.Name} but the request is for {Symbol}.");
        }

        if (!symbol.IsTradingEnabled)
        {
            return TradeErrors.SymbolNotTradable(Symbol);
        }

        var normalized = this;

        if (Price is { } price)
        {
            var priceResult = symbol.NormalizePrice(price, rounding);

            if (!priceResult.TryGetValue(out var normalizedPrice))
            {
                return priceResult.Error!;
            }

            normalized = normalized with { Price = normalizedPrice };
        }

        if (TriggerPrice is { } triggerPrice)
        {
            var triggerResult = symbol.NormalizePrice(triggerPrice, rounding);

            if (!triggerResult.TryGetValue(out var normalizedTriggerPrice))
            {
                return triggerResult.Error!;
            }

            normalized = normalized with { TriggerPrice = normalizedTriggerPrice };
        }

        if (ActivationPrice is { } activationPrice)
        {
            var activationResult = symbol.NormalizePrice(activationPrice, rounding);

            if (!activationResult.TryGetValue(out var normalizedActivationPrice))
            {
                return activationResult.Error!;
            }

            normalized = normalized with { ActivationPrice = normalizedActivationPrice };
        }

        if (ClosePosition)
        {
            return normalized;
        }

        var referencePrice = normalized.Price ?? normalized.TriggerPrice ?? normalized.ActivationPrice;

        var quantityResult = referencePrice is { } reference
            ? symbol.NormalizeQuantity(Quantity, reference)
            : symbol.NormalizeQuantity(Quantity);

        return quantityResult.TryGetValue(out var normalizedQuantity)
            ? normalized with { Quantity = normalizedQuantity }
            : quantityResult.Error!;
    }

    private Result ValidateTypeSpecificFields()
    {
        var isTrailing = ConditionalOrderType == ConditionalOrderType.TrailingStopMarket;
        var needsPrice = ConditionalOrderType.PlacesLimitOrder();

        if (needsPrice)
        {
            if (Price is not > 0m)
            {
                return TradeErrors.InvalidOrderRequest($"{ConditionalOrderType} 必須指定大於零的委託價。{ConditionalOrderType} requires a price greater than zero.");
            }

            if (TimeInForce == TimeInForce.Unspecified)
            {
                return TradeErrors.InvalidOrderRequest($"{ConditionalOrderType} 必須指定有效期限規則。{ConditionalOrderType} requires a time in force.");
            }
        }
        else if (Price is not null)
        {
            return TradeErrors.InvalidOrderRequest($"{ConditionalOrderType} 觸發後以市價成交,不可指定委託價。{ConditionalOrderType} fills at market after the trigger and must not carry a price.");
        }

        if (isTrailing)
        {
            if (TriggerPrice is not null)
            {
                return TradeErrors.InvalidOrderRequest($"{ConditionalOrderType} 的觸發價由回撤比例推算,不可自行指定;要延後開始追蹤請用 ActivationPrice。The trigger price of {ConditionalOrderType} is derived from the callback rate and must not be set; use ActivationPrice to delay when trailing starts.");
            }

            if (CallbackRate is not ( > 0m and <= 100m))
            {
                return TradeErrors.InvalidOrderRequest($"{ConditionalOrderType} 的回撤比例必須介於 0 與 100(百分比)之間。The callback rate of {ConditionalOrderType} must be between 0 and 100 percent.");
            }

            return ActivationPrice is not null and not > 0m
                ? TradeErrors.InvalidOrderRequest($"{ConditionalOrderType} 的啟動價必須大於零。The activation price of {ConditionalOrderType} must be greater than zero.")
                : Result.Success();
        }

        if (TriggerPrice is not > 0m)
        {
            return TradeErrors.InvalidOrderRequest($"{ConditionalOrderType} 必須指定大於零的觸發價。{ConditionalOrderType} requires a trigger price greater than zero.");
        }

        if (CallbackRate is not null)
        {
            return TradeErrors.InvalidOrderRequest($"{ConditionalOrderType} 不可指定回撤比例。{ConditionalOrderType} must not carry a callback rate.");
        }

        return ActivationPrice is not null
            ? TradeErrors.InvalidOrderRequest($"{ConditionalOrderType} 不可指定啟動價。{ConditionalOrderType} must not carry an activation price.")
            : Result.Success();
    }
}
