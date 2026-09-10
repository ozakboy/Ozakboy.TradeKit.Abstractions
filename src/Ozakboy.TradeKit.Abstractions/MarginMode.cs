namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 保證金模式。
/// The margin mode of a position.
/// </summary>
public enum MarginMode
{
    /// <summary>
    /// 全倉:整個帳戶的餘額都是這個部位的保證金。
    /// Cross margin: the whole account balance backs the position.
    /// </summary>
    Cross = 0,

    /// <summary>
    /// 逐倉:只有分配給這個部位的保證金會被用掉,爆倉不影響其他部位。
    /// Isolated margin: only the margin allocated to this position is at risk, so a liquidation does not touch
    /// other positions.
    /// </summary>
    Isolated = 1,
}
