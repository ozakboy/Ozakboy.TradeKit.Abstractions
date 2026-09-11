# Changelog

本檔記錄本套件所有值得注意的變更。
All notable changes to this package are documented here.

格式依循 [Keep a Changelog](https://keepachangelog.com/zh-TW/1.1.0/),版號依循
[語意化版本](https://semver.org/lang/zh-TW/)。
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the versioning follows
[Semantic Versioning](https://semver.org/).

## [0.3.0] - 2026-09-12

0.2.0 的 `AccountUpdate` 與 `MarginCall` 用完整的 `Position` / `Balance` 承載事件內容,但交易所的這兩個事件
並不帶全部欄位,拿不到的只能填 0。其中最危險的一個:`Position.Notional` 等於「數量 × 標記價」,
而帳戶變動事件不帶標記價,於是它恆為 0 —— 風控讀到的就是「這個部位沒有曝險」。
0.2.0's `AccountUpdate` and `MarginCall` carried their contents as full `Position` / `Balance` values, but the
exchange events do not deliver every field, so the missing ones had to be zero. The most dangerous: `Position.Notional`
is quantity times mark price, account change events carry no mark price, and so it was always zero — which a risk
check reads as "no exposure".

### 新增功能 / Added

- **`PositionChange`**:帳戶變動事件裡的部位增量。數量、方向、均價、未實現損益、累計已實現損益、保證金模式、
  逐倉保證金。**刻意沒有**標記價、名目價值、槓桿、強平價。
  A position delta from an account change event. **Deliberately without** mark price, notional, leverage, or
  liquidation price.
- **`BalanceChange`**:帳戶變動事件裡的餘額增量。錢包餘額、全倉錢包餘額,以及 `NonTradingChange`
  (入金出金轉帳,不含損益與手續費 —— 權益曲線要扣掉的就是這一項)。**刻意沒有**可用餘額與未實現損益。
  A balance delta: wallet balance, cross wallet balance, and `NonTradingChange` (deposits, withdrawals,
  transfers — what an equity curve must take out). **Deliberately without** available balance or unrealised PnL.
- **`MarginCallPosition`**:保證金追繳裡的部位。追繳事件帶標記價與維持保證金,所以 `Notional` 是真的;
  不帶開倉均價與槓桿,所以沒有這兩個成員。
  A position from a margin call. The event carries mark price and maintenance margin, so `Notional` is real; it
  carries no entry price or leverage, so neither member exists.

### 破壞性變更 / Breaking

- `AccountUpdate.Positions` 由 `IReadOnlyList<Position>` 改為 `IReadOnlyList<PositionChange>`,
  `AccountUpdate.Balances` 由 `IReadOnlyList<Balance>` 改為 `IReadOnlyList<BalanceChange>`,
  `MarginCall.Positions` 由 `IReadOnlyList<Position>` 改為 `IReadOnlyList<MarginCallPosition>`。
  這三個屬性都在 0.2.0(前一天)才加入,發佈時尚無任何已知消費端;趁現在改是代價最低的時刻。
  All three properties arrived in 0.2.0 the day before, with no known consumer yet — the cheapest moment to
  change them.

欄位不存在,編譯器才擋得住誤讀。填 0 加文件警告,靠的是每個讀者都讀過文件。
Only an absent member lets the compiler stop the misreading; zero plus a warning in the docs relies on every reader
having read the docs.

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
