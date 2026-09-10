using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 建立一張委託所需的全部參數。
/// Everything needed to create one order.
/// </summary>
/// <remarks>
/// <para>
/// 這是交易所中立的下單請求:欄位組合的合法性由 <see cref="Validate()"/> 在本地把關,價量的交易規則校正
/// 由 <see cref="NormalizeFor"/> 完成,兩者都不需要網路。實作套件只負責把校驗過的請求翻譯成自家 API 參數。
/// This is the exchange-neutral order request. <see cref="Validate()"/> checks the field combination locally and
/// <see cref="NormalizeFor"/> applies the symbol's trading rules; neither needs a network. An implementation only
/// translates an already validated request into its own API parameters.
/// </para>
/// <para>
/// <see cref="ClientOrderId"/> 是整套系統的冪等識別碼,強烈建議每張單都自己給:送單逾時的時候,
/// 這是唯一能查回「那張單到底進去了沒有」的線索。
/// <see cref="ClientOrderId"/> is the idempotency key for the whole system and should be set on every order: when
/// a submission times out it is the only way to find out whether the order actually reached the exchange.
/// </para>
/// </remarks>
public sealed record OrderRequest
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
    /// 委託類型。
    /// The order type.
    /// </summary>
    public required OrderType OrderType { get; init; }

    /// <summary>
    /// 委託數量,以基礎幣計價。<see cref="ClosePosition"/> 為 <see langword="true"/> 時必須為 0。
    /// The order quantity in base asset units. Must be zero when <see cref="ClosePosition"/> is
    /// <see langword="true"/>.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// 限價,僅限價類型使用。
    /// The limit price, used only by limit-style orders.
    /// </summary>
    public decimal? Price { get; init; }

    /// <summary>
    /// 觸發價,僅停損與停利類型使用。
    /// The trigger price, used only by stop and take-profit orders.
    /// </summary>
    public decimal? StopPrice { get; init; }

    /// <summary>
    /// 移動停損的回撤比例,單位為百分比(例如 <c>1.5</c> 代表 1.5%)。僅
    /// <see cref="OrderType.TrailingStopMarket"/> 使用。
    /// The trailing stop callback rate as a percentage, where <c>1.5</c> means 1.5%. Used only by
    /// <see cref="OrderType.TrailingStopMarket"/>.
    /// </summary>
    public decimal? CallbackRate { get; init; }

    /// <summary>
    /// 有效期限規則,預設 <see cref="TimeInForce.GoodTilCanceled"/>。僅限價類型使用。
    /// The time in force, defaulting to <see cref="TimeInForce.GoodTilCanceled"/>. Used only by limit-style orders.
    /// </summary>
    public TimeInForce TimeInForce { get; init; } = TimeInForce.GoodTilCanceled;

    /// <summary>
    /// 持倉方向,預設 <see cref="PositionSide.Both"/>(單向模式)。
    /// The position side, defaulting to <see cref="PositionSide.Both"/> for one-way mode.
    /// </summary>
    public PositionSide PositionSide { get; init; } = PositionSide.Both;

    /// <summary>
    /// 條件單看哪一種價格觸發,預設標記價。
    /// Which price a conditional order watches; defaults to the mark price.
    /// </summary>
    public TriggerPriceType TriggerPriceType { get; init; } = TriggerPriceType.MarkPrice;

    /// <summary>
    /// 只減倉:這張單只能縮小既有部位,不能加倉或反向開倉。
    /// Reduce only: the order may only shrink an existing position, never add to it or flip it.
    /// </summary>
    public bool ReduceOnly { get; init; }

    /// <summary>
    /// 全部平倉:由交易所以當下的部位規模成交,不指定數量。
    /// Close position: the exchange sizes the order from the current position instead of an explicit quantity.
    /// </summary>
    public bool ClosePosition { get; init; }

    /// <summary>
    /// 用戶端訂單編號(冪等識別碼)。留白時由實作套件產生。
    /// The client order id, which acts as the idempotency key. Implementations generate one when it is left unset.
    /// </summary>
    public string? ClientOrderId { get; init; }

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

        if (OrderType == OrderType.Unspecified)
        {
            return TradeErrors.InvalidOrderRequest("必須指定委託類型。The order type must be specified.");
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
        }
        else if (Quantity <= 0m)
        {
            return TradeErrors.InvalidOrderRequest("數量必須大於零。Quantity must be greater than zero.");
        }

        return ValidateTypeSpecificFields();
    }

    /// <summary>
    /// 依交易對的交易規則校正價量,回傳可直接送出的請求。
    /// Applies the symbol's trading rules to the price and quantity, returning a request that is ready to send.
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
    /// 最小名目價值以 <see cref="Price"/>(沒有就用 <see cref="StopPrice"/>)檢查。市價單兩者都沒有,
    /// 這時只做數量對齊,名目價值請呼叫端自己用
    /// <see cref="SymbolInfo.NormalizeQuantity(decimal, decimal)"/> 搭配標記價檢查。
    /// The minimum notional is checked against <see cref="Price"/>, falling back to <see cref="StopPrice"/>. A
    /// market order has neither, so only the quantity is aligned; check its notional yourself with
    /// <see cref="SymbolInfo.NormalizeQuantity(decimal, decimal)"/> and a mark price.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="symbol"/> 為 <see langword="null"/> 時擲出。
    /// Thrown when <paramref name="symbol"/> is <see langword="null"/>.
    /// </exception>
    public Result<OrderRequest> NormalizeFor(SymbolInfo symbol, PriceRounding rounding = PriceRounding.Nearest)
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

        if (StopPrice is { } stopPrice)
        {
            var stopResult = symbol.NormalizePrice(stopPrice, rounding);

            if (!stopResult.TryGetValue(out var normalizedStopPrice))
            {
                return stopResult.Error!;
            }

            normalized = normalized with { StopPrice = normalizedStopPrice };
        }

        if (ClosePosition)
        {
            return normalized;
        }

        var referencePrice = normalized.Price ?? normalized.StopPrice;

        var quantityResult = referencePrice is { } reference
            ? symbol.NormalizeQuantity(Quantity, reference)
            : symbol.NormalizeQuantity(Quantity);

        return quantityResult.TryGetValue(out var normalizedQuantity)
            ? normalized with { Quantity = normalizedQuantity }
            : quantityResult.Error!;
    }

    private Result ValidateTypeSpecificFields()
    {
        var needsPrice = OrderType is OrderType.Limit or OrderType.StopLimit or OrderType.TakeProfitLimit;
        var needsStopPrice = OrderType is OrderType.StopMarket or OrderType.StopLimit
            or OrderType.TakeProfitMarket or OrderType.TakeProfitLimit;
        var needsCallbackRate = OrderType == OrderType.TrailingStopMarket;

        if (needsPrice)
        {
            if (Price is not > 0m)
            {
                return TradeErrors.InvalidOrderRequest($"{OrderType} 必須指定大於零的價格。{OrderType} requires a price greater than zero.");
            }

            if (TimeInForce == TimeInForce.Unspecified)
            {
                return TradeErrors.InvalidOrderRequest($"{OrderType} 必須指定有效期限規則。{OrderType} requires a time in force.");
            }
        }
        else if (Price is not null)
        {
            return TradeErrors.InvalidOrderRequest($"{OrderType} 不可指定價格。{OrderType} must not carry a price.");
        }

        if (needsStopPrice)
        {
            if (StopPrice is not > 0m)
            {
                return TradeErrors.InvalidOrderRequest($"{OrderType} 必須指定大於零的觸發價。{OrderType} requires a stop price greater than zero.");
            }
        }
        else if (StopPrice is not null)
        {
            return TradeErrors.InvalidOrderRequest($"{OrderType} 不可指定觸發價。{OrderType} must not carry a stop price.");
        }

        if (needsCallbackRate)
        {
            return CallbackRate is > 0m and <= 100m
                ? Result.Success()
                : TradeErrors.InvalidOrderRequest($"{OrderType} 的回撤比例必須介於 0 與 100(百分比)之間。The callback rate of {OrderType} must be between 0 and 100 percent.");
        }

        return CallbackRate is not null
            ? TradeErrors.InvalidOrderRequest($"{OrderType} 不可指定回撤比例。{OrderType} must not carry a callback rate.")
            : Result.Success();
    }
}
