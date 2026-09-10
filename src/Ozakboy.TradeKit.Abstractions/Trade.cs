using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 一筆成交明細(fill)。一張委託可能拆成多筆成交。
/// One fill. A single order can produce many of these.
/// </summary>
/// <remarks>
/// 手續費一定要連同 <see cref="FeeAsset"/> 一起記。合約帳戶可以用不同資產抵扣手續費,
/// 把數字當成計價幣直接從損益裡扣,對帳就會差一截,而且差多少要看當天的抵扣幣價格。
/// A fee is meaningless without its <see cref="FeeAsset"/>. A derivatives account can pay fees in a different
/// asset, so treating the number as quote currency and subtracting it from PnL leaves a gap whose size depends on
/// that asset's price on the day.
/// </remarks>
public sealed record Trade
{
    /// <summary>
    /// 交易對代碼。
    /// The symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 交易所產生的成交編號。
    /// The trade id assigned by the exchange.
    /// </summary>
    public required string TradeId { get; init; }

    /// <summary>
    /// 這筆成交所屬委託的交易所訂單編號。
    /// The exchange order id of the order this fill belongs to.
    /// </summary>
    public string? ExchangeOrderId { get; init; }

    /// <summary>
    /// 這筆成交所屬委託的用戶端訂單編號。
    /// The client order id of the order this fill belongs to.
    /// </summary>
    public string? ClientOrderId { get; init; }

    /// <summary>
    /// 買賣方向。
    /// The side of the fill.
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// 持倉方向,單向模式為 <see cref="PositionSide.Both"/>。
    /// The position side; <see cref="PositionSide.Both"/> in one-way mode.
    /// </summary>
    public PositionSide PositionSide { get; init; } = PositionSide.Both;

    /// <summary>
    /// 成交價。
    /// The fill price.
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// 成交數量,以基礎幣計。
    /// The filled quantity in base asset units.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// 手續費金額。正數代表付出,負數代表返佣。
    /// The fee amount. Positive means paid; negative means a rebate.
    /// </summary>
    public decimal Fee { get; init; }

    /// <summary>
    /// 手續費計價的資產代碼。
    /// The asset the fee is denominated in.
    /// </summary>
    public string FeeAsset { get; init; } = string.Empty;

    /// <summary>
    /// 這筆成交結算的已實現損益,以計價幣計。開倉時為 0。
    /// The realised profit and loss settled by this fill, in quote asset units; zero when opening a position.
    /// </summary>
    public decimal RealizedPnl { get; init; }

    /// <summary>
    /// 是否為掛單方(Maker)。掛單方通常享有較低甚至為負的手續費率。
    /// Whether this side was the maker, which usually carries a lower or even negative fee rate.
    /// </summary>
    public bool IsMaker { get; init; }

    /// <summary>
    /// 成交時間(UTC 語意)。
    /// The execution time, in UTC semantics.
    /// </summary>
    public DateTimeOffset ExecutedAt { get; init; }

    /// <summary>
    /// 成交金額,等於成交價乘以成交數量。
    /// The notional of this fill, equal to price times quantity.
    /// </summary>
    public decimal Notional => Price * Quantity;

    /// <summary>
    /// 把手續費轉成帶幣別的金額,避免和計價幣的損益直接相加。
    /// Converts the fee into a currency-aware amount so it cannot be added straight onto quote-currency PnL.
    /// </summary>
    /// <returns>帶幣別的手續費。The fee with its currency attached.</returns>
    /// <exception cref="ArgumentException">
    /// <see cref="FeeAsset"/> 為空白時擲出。
    /// Thrown when <see cref="FeeAsset"/> is blank.
    /// </exception>
    public Money FeeAsMoney() => new(Fee, FeeAsset);
}
