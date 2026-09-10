namespace Ozakboy.TradeKit.Abstractions.Tests;

/// <summary>
/// 錯誤代碼與錯誤建立方法的測試。
/// Tests for the error codes and the error factory methods.
/// </summary>
[TestClass]
public sealed class TradeErrorsTests
{
    [TestMethod]
    public void 找不到類的錯誤分類為NotFound()
    {
        Assert.AreEqual(ErrorCategory.NotFound, TradeErrors.SymbolNotFound("BTCUSDT").Category);
        Assert.AreEqual(ErrorCategory.NotFound, TradeErrors.PositionNotFound("BTCUSDT").Category);
        Assert.AreEqual(ErrorCategory.NotFound, TradeErrors.BalanceNotFound("USDT").Category);
        Assert.AreEqual(ErrorCategory.NotFound, TradeErrors.OrderNotFound(OrderIdentifier.FromClientId("x")).Category);
    }

    [TestMethod]
    public void 驗證類的錯誤分類為Validation且不可重試()
    {
        var errors = new[]
        {
            TradeErrors.InvalidQuantity(0m),
            TradeErrors.QuantityBelowMinimum(0.0001m, 0.001m),
            TradeErrors.QuantityAboveMaximum(2000m, 1000m),
            TradeErrors.NotionalBelowMinimum(50m, 100m),
            TradeErrors.InvalidPrice(0m),
            TradeErrors.PriceOutOfRange(1m, 10m, 20m),
            TradeErrors.InvalidOrderRequest("測試。Test."),
            TradeErrors.InvalidSymbolRules("測試。Test."),
            TradeErrors.InvalidQuery("測試。Test."),
            TradeErrors.UnsupportedInterval("7m"),
        };

        foreach (var error in errors)
        {
            Assert.AreEqual(ErrorCategory.Validation, error.Category, error.Code);
            Assert.IsFalse(error.IsTransient, error.Code);
        }
    }

    [TestMethod]
    public void 錯誤代碼一律以trade為前綴()
    {
        var codes = new[]
        {
            TradeErrorCodes.SymbolNotFound,
            TradeErrorCodes.SymbolNotTradable,
            TradeErrorCodes.InvalidSymbolRules,
            TradeErrorCodes.UnsupportedInterval,
            TradeErrorCodes.InvalidQuantity,
            TradeErrorCodes.QuantityBelowMinimum,
            TradeErrorCodes.QuantityAboveMaximum,
            TradeErrorCodes.NotionalBelowMinimum,
            TradeErrorCodes.InvalidPrice,
            TradeErrorCodes.PriceOutOfRange,
            TradeErrorCodes.InvalidOrderRequest,
            TradeErrorCodes.InvalidQuery,
            TradeErrorCodes.OrderNotFound,
            TradeErrorCodes.DuplicateClientOrderId,
            TradeErrorCodes.OrderRejected,
            TradeErrorCodes.OrderNotCancelable,
            TradeErrorCodes.PositionNotFound,
            TradeErrorCodes.ReduceOnlyRejected,
            TradeErrorCodes.BalanceNotFound,
            TradeErrorCodes.InsufficientBalance,
            TradeErrorCodes.InsufficientMargin,
            TradeErrorCodes.LeverageNotAllowed,
            TradeErrorCodes.MarginModeRejected,
            TradeErrorCodes.RateLimited,
            TradeErrorCodes.InvalidSignature,
            TradeErrorCodes.InvalidCredentials,
            TradeErrorCodes.PermissionDenied,
            TradeErrorCodes.TimestampOutOfSync,
            TradeErrorCodes.ExchangeUnavailable,
            TradeErrorCodes.MarketClosed,
            TradeErrorCodes.NetworkFailure,
            TradeErrorCodes.RequestTimeout,
            TradeErrorCodes.StreamDisconnected,
            TradeErrorCodes.SubscriptionFailed,
            TradeErrorCodes.NotSupported,
            TradeErrorCodes.UnknownExchangeError,
        };

        foreach (var code in codes)
        {
            StringAssert.StartsWith(code, "trade.");
            Assert.AreEqual(code.ToLowerInvariant(), code, "錯誤代碼一律小寫");
        }

        Assert.AreEqual(codes.Length, codes.Distinct().Count(), "錯誤代碼不可重複");
    }

    [TestMethod]
    public void 錯誤訊息帶得出實際數值且不用科學記號()
    {
        var error = TradeErrors.QuantityBelowMinimum(0.00001m, 0.001m);

        StringAssert.Contains(error.Message, "0.00001");
        StringAssert.Contains(error.Message, "0.001");
        Assert.IsFalse(error.Message.Contains("E-", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void 不可交易與不支援的操作屬於狀態衝突()
    {
        Assert.AreEqual(ErrorCategory.Conflict, TradeErrors.SymbolNotTradable("BTCUSDT").Category);
        Assert.AreEqual(ErrorCategory.Conflict, TradeErrors.NotSupported("變更保證金模式").Category);
    }

    [TestMethod]
    public void 建立錯誤時不接受空參數()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TradeErrors.SymbolNotFound(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TradeErrors.SymbolNotTradable(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TradeErrors.PositionNotFound(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TradeErrors.BalanceNotFound(null!));
        _ = Assert.ThrowsExactly<ArgumentException>(() => TradeErrors.InvalidOrderRequest(" "));
        _ = Assert.ThrowsExactly<ArgumentException>(() => TradeErrors.InvalidSymbolRules(" "));
        _ = Assert.ThrowsExactly<ArgumentException>(() => TradeErrors.InvalidQuery(" "));
        _ = Assert.ThrowsExactly<ArgumentException>(() => TradeErrors.NotSupported(" "));
    }
}
