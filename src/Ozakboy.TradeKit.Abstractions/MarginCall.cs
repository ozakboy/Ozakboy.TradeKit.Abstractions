namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 保證金追繳警告:交易所認為這些部位已經逼近強平。
/// A margin call: the exchange considers these positions close to liquidation.
/// </summary>
/// <remarks>
/// <para>
/// 這是交易所會主動送出的最後一次提醒,下一步就是強平。收到之後該做的是降險——減倉或補保證金,
/// 而不是記一筆日誌了事。
/// This is the last warning the exchange volunteers; what follows it is liquidation. The response is to reduce
/// risk — trim the position or add margin — not to write a log line.
/// </para>
/// <para>
/// <see cref="Positions"/> 只含被警告的那些部位,與帳戶裡其他部位無關。
/// <see cref="Positions"/> holds only the positions being warned about, and says nothing about the others.
/// </para>
/// </remarks>
public sealed record MarginCall
{
    /// <summary>
    /// 全倉錢包餘額。交易所未提供時為 <see langword="null"/>。
    /// The cross wallet balance, or <see langword="null"/> when the exchange does not provide it.
    /// </summary>
    public decimal? CrossWalletBalance { get; init; }

    /// <summary>
    /// 被警告的部位。
    /// The positions under warning.
    /// </summary>
    public IReadOnlyList<Position> Positions { get; init; } = [];

    /// <summary>
    /// 這筆警告的時間(UTC 語意)。
    /// The timestamp of this warning, in UTC semantics.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}
