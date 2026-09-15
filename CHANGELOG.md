# Changelog

本檔記錄本套件所有值得注意的變更。
All notable changes to this package are documented here.

格式依循 [Keep a Changelog](https://keepachangelog.com/zh-TW/1.1.0/),版號依循
[語意化版本](https://semver.org/lang/zh-TW/)。
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the versioning follows
[Semantic Versioning](https://semver.org/).

## [0.5.0] - 2026-09-15

帳戶模型補上**維持保證金**。保證金率(保證金餘額 ÷ 維持保證金)是判斷離強平還有多遠的標準算法,
但先前的 `Balance` 與 `AccountSnapshot` 都沒有這個數字,消費端只能拿起始保證金代替 —— 起始保證金遠大於
維持保證金,算出來的保證金率偏低,風控會比實際需要更早擋單。
The account model gains the **maintenance margin**. A margin ratio — margin balance divided by maintenance
margin — is the standard measure of distance from liquidation, yet neither `Balance` nor `AccountSnapshot` carried
that figure, leaving consumers to substitute the initial margin. The initial margin is far larger, so the ratio
came out low and risk control blocked orders earlier than it had to.

### 新增功能 / Added

- **`Balance.MaintenanceMargin`、`Balance.InitialMargin`**(`decimal?`):單一資產的維持保證金與起始保證金
  (持倉與掛單合計)。
  The maintenance margin and the initial margin (positions and open orders together) of one asset.
- **`AccountSnapshot.TotalMaintenanceMargin`、`AccountSnapshot.TotalInitialMargin`**(`decimal?`):整個帳戶的總額,
  以交易所彙總帳戶時的計價單位表示。多資產保證金模式下交易所會先換算再加總,所以它不一定等於任何單一資產的值;
  只看單一保證金資產時請用該資產的 `Balance.MaintenanceMargin`。
  Account-wide totals in whatever unit the exchange aggregates the account in. In a multi-asset margin mode the
  exchange converts before summing, so a total need not equal any single asset's figure; for one margin asset, use
  that asset's `Balance.MaintenanceMargin`.

四個欄位都是 **`null` 表示交易所未提供,`0` 表示交易所明確回報為零**(例如空手時)。刻意不用 `decimal`
預設 0:缺值填 0 會讓風控讀成「沒有維持保證金需求」,也就是這個帳戶沒有強平風險。
For all four, **`null` means the exchange did not provide it and `0` means the exchange reported zero** — when
flat, say. A plain `decimal` defaulting to zero was ruled out on purpose: a missing value filled with zero reads as
"no maintenance margin required", which is to say no liquidation risk.

### 對實作者的影響 / For implementers

**不是破壞性變更。**新增的都是預設 `null` 的 `init` 屬性,既有的物件初始設定式照樣編得過,沒有設定的實作
得到的是 `null`(未知),不是一個看起來像真的 0。能取得這些數字的實作應該填上;取不到的就留 `null`,
不要為了「欄位有值」而填 0。回測撮合器若自己計算保證金,可以填入計算值。
兩個型別都是 record,新屬性會納入值相等比較與 `ToString()` 的輸出。
**Not a breaking change.** The additions are `init` properties defaulting to `null`, so existing object
initialisers still compile, and an implementation that sets nothing yields `null` — unknown — rather than a
plausible-looking zero. Implementations that can obtain the figures should fill them in; those that cannot should
leave them `null` rather than writing zero just to have a value. A backtest matcher that computes margin itself may
supply its computed figures. Both types are records, so the new properties take part in value equality and in
`ToString()` output.

## [0.4.0] - 2026-09-14

條件單(停損、停利、移動停損)先前只是 `OrderType` 上的幾個列舉值,與一般委託共用 `PlaceOrderAsync`。
交易所已經不是這樣看它了:條件單被移到獨立的服務底下,編號自成一套、撤單端點不同、狀態機也不一樣,
舊的下單端點對這幾個型別一律拒單。本版把這條路徑獨立出來 —— 停損掛不上去而沒被發現,是這套抽象層
最不能容許的失敗。
Conditional orders — stops, take-profits, trailing stops — used to be a few members of `OrderType` sharing
`PlaceOrderAsync` with ordinary orders. Exchanges no longer see them that way: they have been moved to a separate
service with their own numbering, a different cancellation endpoint, and a different state machine, and the old
order endpoint now rejects those types outright. This release gives that path its own surface — a stop that
silently failed to be placed is the one failure this abstraction layer must not allow.

### 新增功能 / Added

- **`ConditionalOrderRequest`**:條件單的下單請求。觸發價、觸發價基準、數量或全部平倉、只減倉、
  觸發後的限價與有效期限、移動停損的回撤比例與啟動價、`ClientConditionalOrderId`。
  與 `OrderRequest` 一樣有本地的 `Validate()` 與 `NormalizeFor()`,後者會把**觸發價、委託價與啟動價
  一併對齊到跳動點** —— 觸發價沒對齊一樣會被拒單,而停損被拒單是最不該發生的那一種。
  The conditional order request, with local `Validate()` and `NormalizeFor()` as on `OrderRequest`. The latter
  aligns **the trigger price, the limit price, and the activation price** to the tick size: an unaligned trigger
  price is rejected like any other, and a rejected stop is the one rejection that must not happen.
- **`ConditionalOrder`**:條件單的狀態快照。刻意**沒有**已成交數量與均價 —— 觸發之前沒有成交可言,
  觸發之後成交屬於 `TriggeredOrderId` 指向的那張委託。擺一組恆為 0 的成交欄位在這裡,
  只會讓「還沒觸發」與「觸發了但沒成交」看起來一模一樣。
  A snapshot of a conditional order, deliberately **without** fill quantity or average price: before the trigger
  there is nothing to fill, and afterwards the fills belong to the order named by `TriggeredOrderId`. Permanently
  zero fill fields here would make "not triggered yet" and "triggered but unfilled" look identical.
- **`ConditionalOrderUpdate`**:私有串流送出的條件單狀態變化。除了快照之外還帶 `RawStatus` 與
  `RejectReason` —— 停損被交易所拒絕是會出人命的事(以為有保護、其實沒有),而拒絕原因只在事件裡
  出現一次,事後再查那張單只會得到一句「已拒絕」。
  A conditional order state change from the private stream. Besides the snapshot it carries `RawStatus` and
  `RejectReason`: a stop rejected by the exchange is the dangerous case — protection believed to be in place that
  is not — and the reason appears exactly once, in the event.
- **`ConditionalOrderType`** 與 **`ConditionalOrderStatus`**(含 `IsOpen()` / `IsFinal()` 擴充方法):
  `Triggering` 與 `Triggered` 都算「仍然有效」,因為那張觸發出來的委託還沒成交完,把它當成已結束
  會讓緊急出場漏撤一張。兩者的零值都是 `Unspecified`。
  With `IsOpen()` / `IsFinal()` helpers. `Triggering` and `Triggered` both count as live, because the order they
  produced has not finished filling and treating them as done leaves that order uncancelled on an emergency
  exit. Both enums have `Unspecified` as their zero value.

  狀態列舉裡有兩個值容易被當成多餘,它們各自擋住一種誤讀:
  Two of the states look redundant and each stops a specific misreading:

  - **`Triggering`**(已送往撮合引擎、尚未被接受)與 `Triggered` 分開,是因為這一段仍可能以
    `Rejected` 收場 —— 條件單在觸發**之前**通常不做保證金檢查,檢查就發生在這一刻。
    Kept apart from `Triggered` because this stage can still end in `Rejected`: a conditional order is
    typically not margin-checked **before** it triggers, and the check happens here.
  - **`Finished`**(觸發後的委託結束了,但交易所沒說是成交還是被撤)不是 `Filled` 的同義詞。
    交易所若只講到這裡,要知道究竟成交多少必須拿 `TriggeredOrderId` 去查那張委託;
    把它讀成成交,會讓一張觸發後被撤掉的停損在帳上變成一次不存在的平倉。
    Not a synonym for `Filled`. When the exchange reports only this much, finding out how much actually filled
    means looking the resulting order up by `TriggeredOrderId`; reading it as a fill turns a stop that was
    cancelled after triggering into a close that never happened.
- **`ConditionalOrderIdentifier`**:條件單專屬的識別碼(交易所編號或用戶端編號擇一)。
  與 `OrderIdentifier` 分成兩個型別,因為條件單編號與委託編號是交易所兩套獨立的號碼 ——
  拿停損的編號去查一般委託只會得到「找不到」,而型別分開之後那個錯誤在編譯期就過不了。
  A dedicated identifier. It is a separate type from `OrderIdentifier` because the two numbering schemes are
  independent at the exchange: looking a stop up through the plain order endpoint only ever returns "not found",
  and with separate types that mistake no longer compiles.
- **`ConditionalOrderTypeExtensions.ToOrderType()`**:把條件單類型對映回 `OrderType`,供回測撮合器
  共用同一套成交邏輯。
  Maps a conditional type back to `OrderType` so a backtest matcher can reuse one set of fill logic.
- **五個中立錯誤代碼 / five neutral error codes**:`trade.conditional_order_not_found`、
  `trade.conditional_order_rejected`、`trade.duplicate_client_conditional_order_id`、
  `trade.conditional_order_limit_exceeded`、`trade.conditional_order_path_required`,
  以及 `TradeErrors.ConditionalOrderNotFound()` / `TradeErrors.ConditionalOrderPathRequired()`。
  「找不到條件單」與「找不到委託」刻意不共用代碼:共用了,上層就分不出「停損不見了」與「進場單不見了」,
  而前者代表部位正在裸奔。
  "Conditional order not found" deliberately does not share a code with "order not found": sharing leaves callers
  unable to tell "the stop is gone" from "the entry is gone", and the first of those means a position is
  currently unprotected.
- **`IExchangeClient.GetUserTradesAsync(symbol, since, fromId, limit, cancellationToken)`**:查詢帳戶在某個
  商品上的成交紀錄,`since`(UTC)與 `fromId` 擇一當起點,兩個都給是失敗而不是靜默挑一個,`limit` 的上限
  由交易所決定。它是給**對帳**用的,不是給畫面看歷史用的:成交平常由
  `IUserDataFeed.SubscribeTradeUpdatesAsync` 推過來,而串流會斷 —— 斷線期間的成交沒有任何人補,部位與
  已實現損益就從那一刻起一路錯下去,而畫面與日誌都看不出來。引擎定期(例如每 5 分鐘)以及每次重連之後,
  以本地最後一筆成交的時間當起點回頭補查。補查**一定會與已收到的重疊**,因為起點取的是那一筆的時間本身
  而不是之後一瞬間 —— 同一毫秒內的第二筆成交一旦被跳過就永遠補不回來,所以呼叫端必須以 `Trade.TradeId` 去重。
  A query for the account's own fills on one symbol, taking either `since` (UTC) or `fromId` as its cursor —
  supplying both fails rather than silently picking one — with `limit` capped by the exchange. It exists for
  **reconciliation**, not for displaying history: fills normally arrive on
  `IUserDataFeed.SubscribeTradeUpdatesAsync`, that stream drops, and nothing else backfills what happened while
  it was down, so the position and the realised P&L stay wrong from then on with nothing on screen to show it.
  The engine sweeps periodically and after every reconnection, starting from the timestamp of the last fill it
  holds. A sweep **always overlaps** with what was already received, because the cursor is that fill's own
  timestamp rather than an instant after it — a second fill inside the same millisecond would otherwise be lost
  for good — so callers must de-duplicate on `Trade.TradeId`.

  `Trade` 本身沒有變動:對帳需要的欄位它原本就齊了(`TradeId`、`ExchangeOrderId`、`Side`、`Price`、
  `Quantity`、`Fee` + `FeeAsset`、`RealizedPnl`、`IsMaker`、`ExecutedAt`)。
  `Trade` is unchanged: it already carries everything reconciliation needs.

### 破壞性變更 / Breaking

- **`IExchangeClient` 新增五個成員**:`PlaceConditionalOrderAsync`、`CancelConditionalOrderAsync`、
  `GetConditionalOrderAsync`、`GetOpenConditionalOrdersAsync`、`CancelAllConditionalOrdersAsync`。
  介面新增成員對既有實作是破壞性的 —— 回測撮合器與任何自訂實作都要補上這五個。
  `PlaceConditionalOrderAsync` 的 XML 註解寫明了撮合器該怎麼實作它:條件單在 `New` 期間不佔簿上的位置、
  不吃保證金,觸發與否由 `TriggerPriceType` 指定的價格判定,**回測資料只有成交價時必須講明,
  不可以拿成交價冒充標記價** —— 那會讓實盤看標記價的停損在回測裡被 K 線影線掃出場。
  Adding members to an interface breaks every existing implementation: a backtest matcher and any custom client
  must supply all five. The XML docs on `PlaceConditionalOrderAsync` spell out the matcher semantics, including
  that a backtest holding only traded prices **must say so rather than passing them off as mark prices**.
- **`IExchangeClient` 另新增 `GetUserTradesAsync`**(見上)。同樣是介面新增成員,自訂實作與回測撮合器
  都要補上;撮合器本來就有每一筆成交的完整資料,補的是一個對自己那份清單的過濾。
  Another added member, so custom clients and backtest matchers must supply it too. A matcher already holds every
  fill it produced, so what it supplies is a filter over its own list.
- **`IUserDataFeed` 新增 `SubscribeConditionalOrderUpdatesAsync`**。條件單的狀態變化**不會**出現在
  `SubscribeOrderUpdatesAsync`,只訂閱委託更新的話,「停損被觸發了」這件事會整個消失。
  Conditional order state changes do **not** appear on `SubscribeOrderUpdatesAsync`; subscribing only to order
  updates loses the fact that a stop triggered at all.
- **`PlaceOrderAsync` 的合約收窄**:它只收非條件單。停損與停利送到這裡會被交易所拒絕,
  實作應以 `trade.conditional_order_path_required` 先擋下來。這不是新的限制,是把交易所已經在做的事
  寫進契約 —— 先前送得出去、但一定會拿回一個看不出原因的參數錯誤。
  `PlaceOrderAsync` now takes non-conditional orders only. This is not a new restriction but the contract catching
  up with what exchanges already do: the call used to go out and come back as an uninformative parameter error.

仍是 0.x 階段,依既有慣例以 Minor 升版承載這次的介面變更。
Still in 0.x, so this interface change rides a minor bump as before.

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
