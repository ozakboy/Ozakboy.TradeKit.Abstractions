namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 價格對齊到最小跳動點時的取整方向。
/// The direction used when aligning a price to the tick size.
/// </summary>
/// <remarks>
/// 價格沒有像數量那樣「一律向下」的安全方向:買單向下取整是划算的,賣單向下取整卻是少賺。
/// 因此方向交由呼叫端明講,不由本套件替它猜。
/// Unlike quantities, prices have no universally safe direction: rounding down favours a buy order but costs a
/// sell order. The caller therefore states the direction explicitly instead of letting this library guess.
/// </remarks>
public enum PriceRounding
{
    /// <summary>
    /// 取最接近的跳動點(預設)。
    /// Round to the nearest tick, which is the default.
    /// </summary>
    Nearest = 0,

    /// <summary>
    /// 向下取整到跳動點。買方掛單想壓低成本時用。
    /// Round down to a tick; used when a buyer wants to shade the price down.
    /// </summary>
    Down = 1,

    /// <summary>
    /// 向上取整到跳動點。賣方掛單想墊高報價時用。
    /// Round up to a tick; used when a seller wants to shade the price up.
    /// </summary>
    Up = 2,
}
