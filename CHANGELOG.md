# Changelog

本檔記錄本套件所有值得注意的變更。
All notable changes to this package are documented here.

格式依循 [Keep a Changelog](https://keepachangelog.com/zh-TW/1.1.0/),版號依循
[語意化版本](https://semver.org/lang/zh-TW/)。
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the versioning follows
[Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-09-11

`IUserDataFeed` 先前只收得下委託與成交,但私有串流實際會送四類事件。缺的三類分別是餘額與部位的變動、
保證金追繳,以及「串流斷過、本地狀態已經不可信」—— 沒有它們,部位追蹤會在無聲中與交易所分岔。
`IUserDataFeed` previously admitted only orders and fills, while a private stream actually carries four kinds of
event. The three missing ones are account changes, margin calls, and "the stream was interrupted and local state
can no longer be trusted" — without them, position tracking drifts from the exchange in silence.

### 新增功能 / Added

- **`IUserDataFeed` 三個訂閱方法 / three subscriptions**:`SubscribeAccountUpdatesAsync`、
  `SubscribeMarginCallsAsync`、`SubscribeResyncSignalsAsync`。
- **`AccountUpdate` 與 `AccountUpdateReason`**:帳戶變動的**增量**,只列這次動到的餘額與部位,
  並帶上變動原因(成交、資金費、入金、出金、保證金轉帳、保證金模式切換、強平、調整、自動兌換)。
  刻意與 `AccountSnapshot` 分成兩個型別:把增量當快照讀,會把「沒被提到的餘額」誤讀成零。
  原始代碼保留在 `RawReason`,列舉收不下的值落在 `Unknown` 而不是任何有意義的值。
  A **delta** of an account change carrying only what moved, plus the reason it moved. It is deliberately a
  separate type from `AccountSnapshot`: read as a snapshot, "not mentioned" silently becomes "zero". The
  exchange's own code is kept in `RawReason`, and anything unrecognised lands on `Unknown`.
- **`MarginCall`**:被警告的部位與全倉錢包餘額。這是交易所主動送出的最後一次提醒,下一步就是強平。
  The positions under warning and the cross wallet balance — the last warning before liquidation.
- **`ResyncRequired` 與 `ResyncReason`**:「本地狀態從這一刻起不可信,請全量對帳」。
  刻意不綁在任何一家交易所的名詞上:幣安的 `listenKeyExpired` 是來源之一,但**斷線重連**後果一樣、
  而且常見得多,兩者走同一條訊號,消費端就不會只處理了罕見的那一個。
  `UntrustedSince` 與 `Timestamp` 分開記錄 —— 要補的資料從斷線那一刻起算,不是從發現的那一刻。
  Deliberately not tied to one exchange's vocabulary: a Binance `listenKeyExpired` is one source, but a plain
  reconnect has identical consequences and happens far more often. `UntrustedSince` and `Timestamp` are separate,
  because the gap to backfill starts when the stream dropped, not when that was noticed.

### 技術改進 / Technical

- 相依的 `Ozakboy.Core.Abstractions` 由 0.2.0 升至 0.3.0。
  The `Ozakboy.Core.Abstractions` dependency moves from 0.2.0 to 0.3.0.

### 對實作者的影響 / For implementers

介面新增方法,對**實作 `IUserDataFeed` 的型別**是破壞性的 —— 既有實作要補上三個方法才編得過。
對只**呼叫**這個介面的程式碼沒有影響。回測那種不會斷線、不會被追繳的實作,後兩條回空串流即可。
Adding methods to an interface breaks **implementers** — an existing implementation needs the three new members
to compile again. Callers are unaffected. An implementation that cannot drop a connection or be margin-called,
such as a backtest, returns empty streams for the latter two.

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
