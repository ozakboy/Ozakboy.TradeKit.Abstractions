# Ozakboy.TradeKit.Abstractions

.NET 的合約交易所中立抽象層。

[English](README.md)

這個套件定義「一個合約交易所應該長什麼樣」:委託、持倉、帳戶、K 線、成交,以及管住這些東西的交易規則。
策略與回測程式碼因此只依賴交易所的「形狀」,而不是某一家交易所的客戶端函式庫。

裡面只有型別定義與純函式。沒有 HTTP、沒有 WebSocket、沒有簽章,也沒有任何一家交易所的專屬程式碼。

## 為什麼需要它

為了回測。回測引擎必須能注入一個假的交易所,而策略如果直接呼叫具體的交易所客戶端就辦不到。
這裡每一個介面都照同一條硬性驗收標準設計:

> **一個純記憶體的模擬撮合器必須能完整實作它。**

API 裡沒有任何一處假設你有網路連線、有 WebSocket 或有 API 金鑰。測試專案裡附了一個實作了全部介面的
記憶體版交易所,長期守住這條標準。

## 安裝

```
dotnet add package Ozakboy.TradeKit.Abstractions
```

目標框架 `net10.0`,只相依
[`Ozakboy.Core.Abstractions`](https://github.com/ozakboy/Ozakboy.Core.Abstractions),不使用任何第三方套件。

## 設計原則

| 原則 | 理由 |
| --- | --- |
| 會失敗的操作一律回傳 `Result<T>` | 拒單、限流、查無訂單是每天都會遇到的正常情況,不是例外。例外只留給程式缺陷。 |
| 價格、數量、金額一律 `decimal` | 二進位浮點數表示不了 0.1,誤差會變成真實的下單金額差異。 |
| 時間一律 `DateTimeOffset`,語意為 UTC | 混用時區會讓對帳與回測的時間軸錯開,而且錯開的量隨夏令時間變動。 |
| 模型一律不可變(`record` / `readonly record struct` / init-only) | 事後還能被改動的快照沒辦法推理,也重現不了。 |
| 數量一律**向下**對齊步進值 | 向上對齊會讓實際部位大於風控算出來的規模,那是風控破口。 |

## 快速上手

送單前先做交易規則校正:

```csharp
var symbol = new SymbolInfo
{
    Name = "BTCUSDT",
    BaseAsset = "BTC",
    QuoteAsset = "USDT",
    TickSize = 0.1m,
    StepSize = 0.001m,
    MinQuantity = 0.001m,
    MinNotional = 100m,
};

var request = new OrderRequest
{
    Symbol = "BTCUSDT",
    Side = OrderSide.Buy,
    OrderType = OrderType.Limit,
    Price = 50_000.16m,   // 沒對齊跳動點
    Quantity = 0.0025m,   // 沒對齊步進值
    ClientOrderId = "pt-0001",
};

var normalized = request.NormalizeFor(symbol);

if (normalized.TryGetValue(out var ready))
{
    // 價格 50000.2、數量 0.002,名目價值也已對照 MinNotional 檢查過。
    var placed = await exchange.PlaceOrderAsync(ready, cancellationToken);
}
else
{
    // 例如 trade.notional_below_min:這筆單根本下不了。
    logger.LogWarning("{Error}", normalized.Error);
}
```

規模太小的委託會直接回報失敗,而不是回傳一個注定被交易所以一句「參數不合法」擋下來的數值。

## 內容一覽

**列舉** — `OrderSide`、`PositionSide`、`OrderType`、`TimeInForce`、`OrderStatus`、`KlineInterval`、
`MarginMode`、`TriggerPriceType`、`PriceRounding`、`ConditionalOrderType`、`ConditionalOrderStatus`。

**模型** — `SymbolInfo`(識別資料、交易規則與校正方法)、`OrderRequest`、`Order`、`OrderIdentifier`、
`Position`、`Balance`、`AccountSnapshot`(兩者都帶維持保證金與起始保證金,交易所未提供時為 `null`)、`Kline`、`KlineQuery`、`Trade`、`MarkPriceUpdate`、
`NormalizedOrderSize`、`AccountUpdate`(**增量**,不是快照;由只含事件實際欄位的 `PositionChange` 與
`BalanceChange` 組成)、`MarginCall`(內含 `MarginCallPosition`)、`ResyncRequired`,以及條件單這一組:
`ConditionalOrderRequest`、`ConditionalOrder`、`ConditionalOrderUpdate`、`ConditionalOrderIdentifier`。

**介面** — `IExchangeInfoProvider`(商品與伺服器時間)、`IExchangeClient`(帳戶、持倉、委託、條件單)、
`IMarketDataFeed`(歷史 K 線、即時 K 線與標記價)、`IUserDataFeed`(委託、條件單、成交、帳戶變動、
保證金追繳,以及「本地狀態不可信,請全量對帳」的訊號)。

**錯誤代碼** — `TradeErrorCodes` 放中立的代碼(`trade.order_not_found`、`trade.notional_below_min`、
`trade.rate_limited` 等),`TradeErrors` 負責建立對應的 `Error`。把交易所自家的錯誤碼對映到這一組是
實作套件的責任,如此策略裡才不會出現 `if (code == -2011)`。

### 條件單走的是另一條路

停損、停利與移動停損走 `PlaceConditionalOrderAsync`,不走 `PlaceOrderAsync` —— 交易所已經把它們搬到
獨立的服務底下,編號自成一套、撤單端點不同、狀態機也不一樣,舊的下單端點對這幾個型別一律拒單。

三個容易漏掉的後果:

* `GetOpenOrdersAsync` 看不到它們。只用它對帳會得到「沒有任何掛單」的結論,而停損其實好端端地掛在
  另一條路徑上 —— 或者根本不在,兩者長得一模一樣。
* `SubscribeOrderUpdatesAsync` 不帶它們。停損被觸發這件事只出現在
  `SubscribeConditionalOrderUpdatesAsync`;流到委託串流上的是觸發之後那張委託的成交,
  兩者之間要靠 `ConditionalOrder.TriggeredOrderId` 才接得回去。
* `CancelAllOrdersAsync` 撤不掉它們。緊急出場要一併呼叫 `CancelAllConditionalOrdersAsync`,
  否則平倉之後留下來的那張停損會反手開出一個沒人要的反向部位。

### 串流採用 `IAsyncEnumerable<Result<T>>`

訂閱回傳 `IAsyncEnumerable<Result<T>>` 而不是事件:

* 回測實作只要對著存好的 K 線 `yield return` 就完成了;
* `await foreach` 天生循序,策略處理上一根 K 線期間不會被下一根重入;
* 取消與清理由 `CancellationToken` 一併處理,不需要記得 `-=`。

元素是 `Result<T>`,因為斷線重連與單筆訊息解析失敗是即時串流的日常,消費端應該自己決定要記錄後繼續
還是中止,而不是被例外打斷整個迴圈。

## 相關套件

* `Ozakboy.Core.Abstractions` — `Result`、`Money`、`Precision`、`RetryPolicy`。
* `Ozakboy.TradeKit.Binance` — 這組介面的幣安 USDⓈ-M 實作。

## 授權

MIT,見 [LICENSE](LICENSE)。
