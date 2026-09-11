# Changelog

本檔記錄本套件所有值得注意的變更。
All notable changes to this package are documented here.

格式依循 [Keep a Changelog](https://keepachangelog.com/zh-TW/1.1.0/),版號依循
[語意化版本](https://semver.org/lang/zh-TW/)。
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the versioning follows
[Semantic Versioning](https://semver.org/).

## [0.1.1] - 2026-09-11

### Added

- 套件圖示。ozakboy 套件家族共用的品牌圖示,會顯示在 nuget.org 與 IDE 的套件管理員。
  Package icon: the shared ozakboy brand mark now shows on nuget.org and in IDE package managers.

本版沒有任何程式碼變更。NuGet 已發佈版本的套件中繼資料不可更動,要更新圖示只能發新版。
No code changed in this release. NuGet package metadata cannot be altered on an already-published
version, so refreshing the icon requires publishing a new one.

## [0.1.0] - 2026-09-11

首個版本。交易所中立的合約交易抽象層,只含型別定義與純函式,不含任何網路實作。
The first release: exchange-neutral derivatives trading abstractions, containing type definitions and pure
functions only, with no networking of any kind.

### 新增功能 / Added

- **列舉 / Enums**:`OrderSide`、`PositionSide`、`OrderType`、`TimeInForce`、`OrderStatus`、`KlineInterval`、
  `MarginMode`、`TriggerPriceType`、`PriceRounding`。
- **`KlineInterval` 雙向轉換 / conversions**:`ToExchangeString`、`ToTimeSpan`、`TryGetDuration`、
  `IsCalendarBased`、`GetNextOpenTime`,以及 `KlineIntervals.Parse` / `TryParse`(大小寫敏感,`1m` 與 `1M`
  不會互相混淆)。
- **`SymbolInfo` 交易規則校正 / trading-rule normalisation**:`NormalizePrice`、`NormalizeQuantity`、
  `NormalizeOrderSize`、`GetMinimumQuantity`、`Validate`。數量一律向下對齊,低於最小下單量或最小名目價值時
  回報失敗而不是回傳注定被拒的數值。
- **不可變模型 / Immutable models**:`OrderRequest`(含 `Validate` 與 `NormalizeFor`)、`Order`、
  `OrderIdentifier`、`Position`、`Balance`、`AccountSnapshot`、`Kline`、`KlineQuery`、`Trade`、
  `MarkPriceUpdate`、`NormalizedOrderSize`。
- **介面 / Interfaces**:`IExchangeInfoProvider`、`IExchangeClient`、`IMarketDataFeed`、`IUserDataFeed`。
  即時訂閱採用 `IAsyncEnumerable<Result<T>>`,可由純記憶體的回測實作完整滿足。
- **錯誤代碼 / Error codes**:`TradeErrorCodes` 常數與 `TradeErrors` 建立方法,供實作套件把交易所自家的
  錯誤碼對映到中立代碼。

[0.1.0]: https://github.com/ozakboy/Ozakboy.TradeKit.Abstractions/releases/tag/v0.1.0
