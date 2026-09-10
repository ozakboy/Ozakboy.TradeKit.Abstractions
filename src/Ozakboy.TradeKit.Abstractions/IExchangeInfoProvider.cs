using Ozakboy.Core.Abstractions;

namespace Ozakboy.TradeKit.Abstractions;

/// <summary>
/// 提供交易所的商品清單與交易規則。
/// Supplies the exchange's instrument list and trading rules.
/// </summary>
/// <remarks>
/// 獨立成一個介面,是為了讓「只需要交易規則」的元件(部位規模計算、下單前校驗)不必依賴整個
/// <see cref="IExchangeClient"/>,測試時也只要準備一份 <see cref="SymbolInfo"/> 就夠了。
/// This is a separate interface so that components which only need trading rules — position sizing, pre-trade
/// validation — do not have to depend on the whole <see cref="IExchangeClient"/>, and so that a test only has to
/// supply a <see cref="SymbolInfo"/>.
/// </remarks>
public interface IExchangeInfoProvider
{
    /// <summary>
    /// 取得全部商品的交易規則。
    /// Gets the trading rules of every instrument.
    /// </summary>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 商品清單,或失敗原因。
    /// The instrument list, or the reason it failed.
    /// </returns>
    Task<Result<IReadOnlyList<SymbolInfo>>> GetSymbolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得單一商品的交易規則。
    /// Gets the trading rules of one instrument.
    /// </summary>
    /// <param name="symbol">交易對代碼。The symbol.</param>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 該商品的交易規則;不存在時為代碼 <see cref="TradeErrorCodes.SymbolNotFound"/> 的失敗。
    /// The rules for that instrument, or a failure carrying <see cref="TradeErrorCodes.SymbolNotFound"/>.
    /// </returns>
    Task<Result<SymbolInfo>> GetSymbolAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得交易所目前的時間(UTC 語意)。
    /// Gets the exchange's current time, in UTC semantics.
    /// </summary>
    /// <param name="cancellationToken">取消權杖。The cancellation token.</param>
    /// <returns>
    /// 交易所時間,或失敗原因。
    /// The exchange time, or the reason it failed.
    /// </returns>
    /// <remarks>
    /// 用來偵測本機時鐘偏移。簽章型 API 對時間戳的容忍度通常只有幾秒,本機時鐘慢了之後,
    /// 所有請求都會被拒,而回傳的錯誤看起來像簽章錯誤,完全不會指向時鐘。
    /// This exists to detect local clock drift. Signed APIs typically tolerate only a few seconds of skew, and once
    /// the local clock slips every request is rejected with what looks like a signature error and never points at
    /// the clock.
    /// </remarks>
    Task<Result<DateTimeOffset>> GetServerTimeAsync(CancellationToken cancellationToken = default);
}
