using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 一個合約交易對的識別資料與交易規則,並提供把「想要的價量」校正成「交易所會接受的價量」的方法。
/// The identity and trading rules of one derivatives symbol, plus the methods that turn a desired price and
/// quantity into one the exchange will accept.
/// </summary>
/// <remarks>
/// <para>
/// 這是本套件唯一有實質邏輯的型別。交易所對每個商品都規定最小跳動點、數量步進、最小下單量與最小名目價值,
/// 任何一項沒對齊就會被拒單;而拒單訊息通常只有一句「參數不合法」,看不出是哪一項。因此校正一律在送單前
/// 於本地完成,並且在校正結果根本下不了單時明確回報失敗,而不是回傳一個注定被拒絕的數值。
/// This is the only type in the package with real logic. Exchanges impose a tick size, a step size, a minimum
/// quantity, and a minimum notional on every instrument, and reject any order that misses one of them — usually
/// with nothing more informative than "invalid parameter". Normalisation therefore happens locally before
/// submission, and reports an explicit failure when the result cannot be traded at all, instead of handing back a
/// value that is certain to be rejected.
/// </para>
/// <para>
/// 數量一律向下對齊。向上對齊會讓實際部位大於風控算出來的規模,那是風控破口,不是四捨五入問題。
/// Quantities are always aligned downwards. Rounding up makes the real position larger than the size risk
/// management calculated, which is a hole in risk control rather than a rounding preference.
/// </para>
/// <para>
/// 規則本身可能有問題(交易所回應缺欄位、對映寫錯),因此建立之後不自動擲出例外,而是由
/// <see cref="Validate"/> 一次檢查;各校正方法也各自防呆,規則不可用時回傳
/// <see cref="TradeErrorCodes.InvalidSymbolRules"/> 失敗。
/// The rules themselves can be wrong — a missing field in the exchange response, a bad mapping — so construction
/// never throws. Call <see cref="Validate"/> once when loading exchange info; each normalisation method also
/// guards itself and returns a <see cref="TradeErrorCodes.InvalidSymbolRules"/> failure when the rules are unusable.
/// </para>
/// </remarks>
public sealed record SymbolInfo
{
    /// <summary>
    /// 交易對代碼,例如 <c>BTCUSDT</c>。必須與交易所使用的字面值完全相同(含大小寫)。
    /// The symbol code such as <c>BTCUSDT</c>. Must match the exchange's literal exactly, including case.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 基礎幣,例如 <c>BTC</c>。部位數量以這個資產計價。
    /// The base asset such as <c>BTC</c>; position quantities are denominated in it.
    /// </summary>
    public required string BaseAsset { get; init; }

    /// <summary>
    /// 計價幣,例如 <c>USDT</c>。價格、名目價值與保證金以這個資產計價。
    /// The quote asset such as <c>USDT</c>; prices, notional values, and margin are denominated in it.
    /// </summary>
    public required string QuoteAsset { get; init; }

    /// <summary>
    /// 價格的最小變動單位(tick size),必須大於零。
    /// The minimum price increment, known as the tick size. Must be greater than zero.
    /// </summary>
    public required decimal TickSize { get; init; }

    /// <summary>
    /// 數量的最小變動單位(step size),必須大於零。
    /// The minimum quantity increment, known as the step size. Must be greater than zero.
    /// </summary>
    public required decimal StepSize { get; init; }

    /// <summary>
    /// 允許的最小下單量。未設定時為 0,此時真正的下限由 <see cref="StepSize"/> 決定。
    /// The minimum order quantity. When left at zero the real floor comes from <see cref="StepSize"/>.
    /// </summary>
    public decimal MinQuantity { get; init; }

    /// <summary>
    /// 單筆委託允許的最大數量。預設為無上限。
    /// The maximum quantity for a single order. Unlimited by default.
    /// </summary>
    public decimal MaxQuantity { get; init; } = decimal.MaxValue;

    /// <summary>
    /// 允許的最低價格。預設為 0(無下限)。
    /// The lowest accepted price. Zero by default, meaning no floor.
    /// </summary>
    public decimal MinPrice { get; init; }

    /// <summary>
    /// 允許的最高價格。預設為無上限。
    /// The highest accepted price. Unlimited by default.
    /// </summary>
    public decimal MaxPrice { get; init; } = decimal.MaxValue;

    /// <summary>
    /// 允許的最小名目價值(價格 × 數量),以 <see cref="QuoteAsset"/> 計價。預設為 0(無下限)。
    /// The minimum notional value, price times quantity, denominated in <see cref="QuoteAsset"/>. Zero by default.
    /// </summary>
    public decimal MinNotional { get; init; }

    /// <summary>
    /// 這個商品允許的最大槓桿倍數。預設為 1。
    /// The highest leverage allowed on this symbol. Defaults to 1.
    /// </summary>
    public int MaxLeverage { get; init; } = 1;

    /// <summary>
    /// 目前是否可以交易。下架、暫停或僅可平倉的商品為 <see langword="false"/>。
    /// Whether the symbol is currently tradable. Delisted, halted, or reduce-only symbols are
    /// <see langword="false"/>.
    /// </summary>
    public bool IsTradingEnabled { get; init; } = true;

    /// <summary>
    /// 價格的有效小數位數,由 <see cref="TickSize"/> 推得。
    /// The number of decimal places a price may carry, derived from <see cref="TickSize"/>.
    /// </summary>
    public int PriceScale => AreRulesUsable ? Precision.GetSignificantScale(TickSize) : 0;

    /// <summary>
    /// 數量的有效小數位數,由 <see cref="StepSize"/> 推得。
    /// The number of decimal places a quantity may carry, derived from <see cref="StepSize"/>.
    /// </summary>
    public int QuantityScale => AreRulesUsable ? Precision.GetSignificantScale(StepSize) : 0;

    /// <summary>
    /// 真正可以送出的最小數量:<see cref="MinQuantity"/> 與 <see cref="StepSize"/> 取大者。
    /// The smallest quantity that can actually be sent: the larger of <see cref="MinQuantity"/> and
    /// <see cref="StepSize"/>.
    /// </summary>
    /// <remarks>
    /// 小於一個步進的數量向下對齊之後就是零,所以 <see cref="StepSize"/> 本身就是隱含的下限,
    /// 即使交易所回報的 <see cref="MinQuantity"/> 是 0。
    /// Anything smaller than one step floors to zero, so <see cref="StepSize"/> is an implicit floor even when the
    /// exchange reports <see cref="MinQuantity"/> as zero.
    /// </remarks>
    public decimal MinimumTradableQuantity => Math.Max(MinQuantity, StepSize);

    private bool AreRulesUsable => TickSize > 0m && StepSize > 0m;

    /// <summary>
    /// 檢查交易規則本身是否自洽。載入交易所商品資訊時呼叫一次即可。
    /// Checks that the trading rules are internally consistent. Call once when loading exchange info.
    /// </summary>
    /// <returns>
    /// 規則合法時為成功,否則為代碼 <see cref="TradeErrorCodes.InvalidSymbolRules"/> 的失敗。
    /// Success when the rules are valid; otherwise a failure carrying
    /// <see cref="TradeErrorCodes.InvalidSymbolRules"/>.
    /// </returns>
    public Result Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return TradeErrors.InvalidSymbolRules("交易對代碼不可為空白。The symbol name must not be blank.");
        }

        if (string.IsNullOrWhiteSpace(BaseAsset) || string.IsNullOrWhiteSpace(QuoteAsset))
        {
            return TradeErrors.InvalidSymbolRules($"{Name} 的基礎幣與計價幣不可為空白。The base and quote assets of {Name} must not be blank.");
        }

        if (TickSize <= 0m)
        {
            return TradeErrors.InvalidSymbolRules($"{Name} 的價格步進必須大於零。The tick size of {Name} must be greater than zero.");
        }

        if (StepSize <= 0m)
        {
            return TradeErrors.InvalidSymbolRules($"{Name} 的數量步進必須大於零。The step size of {Name} must be greater than zero.");
        }

        if (MinQuantity < 0m || MaxQuantity <= 0m || MinQuantity > MaxQuantity)
        {
            return TradeErrors.InvalidSymbolRules($"{Name} 的數量上下限不合法。The quantity bounds of {Name} are invalid.");
        }

        if (MinPrice < 0m || MaxPrice <= 0m || MinPrice > MaxPrice)
        {
            return TradeErrors.InvalidSymbolRules($"{Name} 的價格上下限不合法。The price bounds of {Name} are invalid.");
        }

        if (MinNotional < 0m)
        {
            return TradeErrors.InvalidSymbolRules($"{Name} 的最小名目價值不可為負。The minimum notional of {Name} must not be negative.");
        }

        return MaxLeverage < 1
            ? TradeErrors.InvalidSymbolRules($"{Name} 的最大槓桿必須至少為 1。The maximum leverage of {Name} must be at least 1.")
            : Result.Success();
    }

    /// <summary>
    /// 判斷價格是否已對齊最小跳動點。
    /// Determines whether a price is already aligned to the tick size.
    /// </summary>
    /// <param name="price">要檢查的價格。The price to check.</param>
    /// <returns>
    /// 已對齊時回傳 <see langword="true"/>;交易規則不可用時一律回傳 <see langword="false"/>。
    /// <see langword="true"/> when aligned; always <see langword="false"/> when the rules are unusable.
    /// </returns>
    public bool IsPriceAligned(decimal price) => AreRulesUsable && Precision.IsAlignedToStep(price, TickSize);

    /// <summary>
    /// 判斷數量是否已對齊數量步進。
    /// Determines whether a quantity is already aligned to the step size.
    /// </summary>
    /// <param name="quantity">要檢查的數量。The quantity to check.</param>
    /// <returns>
    /// 已對齊時回傳 <see langword="true"/>;交易規則不可用時一律回傳 <see langword="false"/>。
    /// <see langword="true"/> when aligned; always <see langword="false"/> when the rules are unusable.
    /// </returns>
    public bool IsQuantityAligned(decimal quantity) => AreRulesUsable && Precision.IsAlignedToStep(quantity, StepSize);

    /// <summary>
    /// 把價格校正成交易所會接受的價格。
    /// Normalises a price into one the exchange will accept.
    /// </summary>
    /// <param name="price">想要的價格。The desired price.</param>
    /// <param name="rounding">
    /// 對齊方向,預設取最接近的跳動點。
    /// The rounding direction; defaults to the nearest tick.
    /// </param>
    /// <returns>
    /// 對齊後的價格;價格非正數、對齊後歸零或超出允許範圍時為失敗。
    /// The aligned price, or a failure when the price is not positive, collapses to zero, or falls outside the
    /// accepted range.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="rounding"/> 為未定義的值時擲出。
    /// Thrown when <paramref name="rounding"/> is an undefined value.
    /// </exception>
    public Result<decimal> NormalizePrice(decimal price, PriceRounding rounding = PriceRounding.Nearest)
    {
        if (!AreRulesUsable)
        {
            return UnusableRules();
        }

        if (price <= 0m)
        {
            return TradeErrors.InvalidPrice(price);
        }

        var normalized = rounding switch
        {
            PriceRounding.Nearest => Precision.RoundToStep(price, TickSize),
            PriceRounding.Down => Precision.FloorToStep(price, TickSize),
            PriceRounding.Up => Precision.CeilingToStep(price, TickSize),
            _ => throw new ArgumentOutOfRangeException(nameof(rounding), rounding, "未定義的取整方向。Undefined rounding direction."),
        };

        if (normalized <= 0m)
        {
            return TradeErrors.InvalidPrice(normalized);
        }

        return normalized < MinPrice || normalized > MaxPrice
            ? TradeErrors.PriceOutOfRange(normalized, MinPrice, MaxPrice)
            : normalized;
    }

    /// <summary>
    /// 把數量向下校正成交易所會接受的數量,不檢查名目價值。
    /// Normalises a quantity downwards into one the exchange will accept, without checking the notional value.
    /// </summary>
    /// <param name="quantity">想要的數量。The desired quantity.</param>
    /// <returns>
    /// 對齊後的數量;低於最小下單量或超過單筆上限時為失敗。
    /// The aligned quantity, or a failure when it falls below the minimum or exceeds the per-order maximum.
    /// </returns>
    /// <remarks>
    /// 超過單筆上限時回報失敗而不是自動截到上限:悄悄把數量改小會讓策略以為部位已經建滿,
    /// 需要拆單的話請由呼叫端明確處理。
    /// Exceeding the maximum is reported as a failure rather than silently clamped: quietly shrinking the quantity
    /// would leave the strategy believing the position is fully built. Splitting the order is the caller's decision.
    /// </remarks>
    public Result<decimal> NormalizeQuantity(decimal quantity)
    {
        if (!AreRulesUsable)
        {
            return UnusableRules();
        }

        if (quantity <= 0m)
        {
            return TradeErrors.InvalidQuantity(quantity);
        }

        var normalized = Precision.FloorToStep(quantity, StepSize);

        if (normalized > MaxQuantity)
        {
            return TradeErrors.QuantityAboveMaximum(normalized, MaxQuantity);
        }

        var minimum = MinimumTradableQuantity;

        return normalized < minimum
            ? TradeErrors.QuantityBelowMinimum(normalized, minimum)
            : normalized;
    }

    /// <summary>
    /// 把數量向下校正,並以參考價檢查最小名目價值。市價單用這個多載。
    /// Normalises a quantity downwards and checks the minimum notional against a reference price. Use this
    /// overload for market orders.
    /// </summary>
    /// <param name="quantity">想要的數量。The desired quantity.</param>
    /// <param name="referencePrice">
    /// 用來估算名目價值的參考價(標記價或最新成交價)。
    /// The reference price used to estimate the notional, typically the mark or last traded price.
    /// </param>
    /// <returns>
    /// 對齊後的數量;數量或名目價值不合格時為失敗。
    /// The aligned quantity, or a failure when either the quantity or the notional falls short.
    /// </returns>
    public Result<decimal> NormalizeQuantity(decimal quantity, decimal referencePrice)
    {
        if (referencePrice <= 0m)
        {
            return TradeErrors.InvalidPrice(referencePrice);
        }

        var quantityResult = NormalizeQuantity(quantity);

        if (!quantityResult.TryGetValue(out var normalized))
        {
            return quantityResult;
        }

        var notional = normalized * referencePrice;

        return notional < MinNotional
            ? TradeErrors.NotionalBelowMinimum(notional, MinNotional)
            : normalized;
    }

    /// <summary>
    /// 一次校正限價單的價格與數量,並檢查最小名目價值。
    /// Normalises the price and quantity of a limit order together and checks the minimum notional.
    /// </summary>
    /// <param name="price">想要的價格。The desired price.</param>
    /// <param name="quantity">想要的數量。The desired quantity.</param>
    /// <param name="rounding">
    /// 價格的對齊方向,預設取最接近的跳動點。
    /// The price rounding direction; defaults to the nearest tick.
    /// </param>
    /// <returns>
    /// 校正後的價量與名目價值;任何一項不合格時為失敗。
    /// The normalised price, quantity, and notional, or a failure when any rule is not met.
    /// </returns>
    public Result<NormalizedOrderSize> NormalizeOrderSize(
        decimal price,
        decimal quantity,
        PriceRounding rounding = PriceRounding.Nearest)
    {
        var priceResult = NormalizePrice(price, rounding);

        if (!priceResult.TryGetValue(out var normalizedPrice))
        {
            return priceResult.Error!;
        }

        var quantityResult = NormalizeQuantity(quantity, normalizedPrice);

        return quantityResult.TryGetValue(out var normalizedQuantity)
            ? new NormalizedOrderSize(normalizedPrice, normalizedQuantity, normalizedPrice * normalizedQuantity)
            : quantityResult.Error!;
    }

    /// <summary>
    /// 算出在指定價格下,同時滿足最小下單量與最小名目價值的最小可送出數量。
    /// Returns the smallest quantity that satisfies both the minimum quantity and the minimum notional at a given
    /// price.
    /// </summary>
    /// <param name="price">用來換算名目價值的價格。The price used to convert the notional.</param>
    /// <returns>
    /// 已對齊步進的最小可送出數量;算出來超過單筆上限時為失敗。
    /// The step-aligned minimum quantity, or a failure when it would exceed the per-order maximum.
    /// </returns>
    /// <remarks>
    /// 部位規模算出來太小而被拒單時,用這個方法可以知道「至少要下多少」,決定是放大到這個數量還是整筆放棄。
    /// When a sized order comes out too small to trade, this tells the caller the floor, so it can decide between
    /// rounding the position up to it and skipping the trade entirely.
    /// </remarks>
    public Result<decimal> GetMinimumQuantity(decimal price)
    {
        if (!AreRulesUsable)
        {
            return UnusableRules();
        }

        if (price <= 0m)
        {
            return TradeErrors.InvalidPrice(price);
        }

        var candidate = MinimumTradableQuantity;

        if (MinNotional > 0m)
        {
            candidate = Math.Max(candidate, Precision.CeilingToStep(MinNotional / price, StepSize));
        }

        candidate = Precision.CeilingToStep(candidate, StepSize);

        // 除法會在最末位產生誤差,對齊後的名目價值可能仍差一點點;補一個步進直到跨過門檻。
        // Division leaves error in the last digit, so the aligned notional can still fall a hair short; add one
        // step at a time until it clears the threshold.
        while (candidate * price < MinNotional)
        {
            candidate += StepSize;

            if (candidate > MaxQuantity)
            {
                return TradeErrors.QuantityAboveMaximum(candidate, MaxQuantity);
            }
        }

        return candidate > MaxQuantity
            ? TradeErrors.QuantityAboveMaximum(candidate, MaxQuantity)
            : candidate;
    }

    /// <summary>
    /// 把價格序列化成可直接送往交易所的字串(無科學記號、無多餘尾隨零)。
    /// Serialises a price into a string ready for the exchange, with no exponent notation and no trailing zeros.
    /// </summary>
    /// <param name="price">要序列化的價格。The price to serialise.</param>
    /// <returns>價格字串。The price string.</returns>
    public static string FormatPrice(decimal price) => Precision.ToPlainString(price);

    /// <summary>
    /// 把數量序列化成可直接送往交易所的字串(無科學記號、無多餘尾隨零)。
    /// Serialises a quantity into a string ready for the exchange, with no exponent notation and no trailing zeros.
    /// </summary>
    /// <param name="quantity">要序列化的數量。The quantity to serialise.</param>
    /// <returns>數量字串。The quantity string.</returns>
    public static string FormatQuantity(decimal quantity) => Precision.ToPlainString(quantity);

    private Error UnusableRules() =>
        TradeErrors.InvalidSymbolRules(
            $"{Name} 的價格或數量步進不合法,無法校正。The tick or step size of {Name} is invalid, so nothing can be normalised.");
}
