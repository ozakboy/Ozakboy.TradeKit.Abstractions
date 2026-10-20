# Ozakboy.TradeKit.Abstractions

Exchange-neutral abstractions for derivatives trading on .NET.

[繁體中文說明](README_zh-TW.md)

This package defines what a futures exchange looks like — orders, positions, accounts, candles, the book, fills,
and the trading rules that govern them — so that strategy and backtest code depends on the shape of an exchange rather
than on one particular exchange's client library.

It contains type definitions and pure functions only. There is no HTTP, no WebSocket, no signing, and no
exchange-specific code anywhere in it.

## Why it exists

Backtesting. A backtest engine has to inject a fake exchange, which is impossible if the strategy talks directly
to a concrete exchange client. Every interface here is designed against one hard acceptance criterion:

> **A purely in-memory matching engine must be able to implement it.**

Nothing in the API presumes a network connection, a WebSocket, or an API key. The test suite includes a small
in-memory exchange that implements every interface, as a standing check on that claim.

## Install

```
dotnet add package Ozakboy.TradeKit.Abstractions
```

Targets `net10.0`. Depends only on [`Ozakboy.Core.Abstractions`](https://github.com/ozakboy/Ozakboy.Core.Abstractions)
— no third-party packages.

## Design rules

| Rule | Reason |
| --- | --- |
| Every fallible operation returns `Result<T>` | Rejections, rate limits, and missing orders are routine, not exceptional. Exceptions are reserved for defects. |
| All prices, quantities, and amounts are `decimal` | Binary floating point cannot represent 0.1, and the error turns into real differences in order size. |
| All timestamps are `DateTimeOffset` with UTC semantics | Mixing time zones skews reconciliation and backtest timelines, and the skew moves with daylight saving. |
| Every model is immutable (`record` / `readonly record struct` / init-only) | Snapshots that can be mutated after the fact cannot be reasoned about or replayed. |
| Quantities are always aligned **down** to the step size | Rounding up makes the real position larger than the size risk management calculated. |

## Quick start

Normalising an order before sending it:

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
    Price = 50_000.16m,   // not on a tick
    Quantity = 0.0025m,   // not on a step
    ClientOrderId = "pt-0001",
};

var normalised = request.NormalizeFor(symbol);

if (normalised.TryGetValue(out var ready))
{
    // Price 50000.2, quantity 0.002, notional checked against MinNotional.
    var placed = await exchange.PlaceOrderAsync(ready, cancellationToken);
}
else
{
    // e.g. trade.notional_below_min — this order cannot be traded at all.
    logger.LogWarning("{Error}", normalised.Error);
}
```

An order that is too small comes back as an explicit failure rather than a value the exchange will reject with an
opaque "invalid parameter".

## What is in the box

**Enums** — `OrderSide`, `PositionSide`, `OrderType`, `TimeInForce`, `OrderStatus`, `KlineInterval`, `MarginMode`,
`TriggerPriceType`, `PriceRounding`, `ConditionalOrderType`, `ConditionalOrderStatus`, `OrderExpiryReason`.

**Models** — `SymbolInfo` (identity plus trading rules and the normalisation methods), `OrderRequest`, `Order` (with
`ExpiryReason`, why the exchange expired or cancelled it — `null` when the exchange gives no reason, never inferred),
`OrderIdentifier`, `Position`, `Balance`, `AccountSnapshot` (both with maintenance and initial margin, `null` when the
exchange does not provide them), `Kline`, `KlineQuery`, `Trade`, `MarkPriceUpdate`, `BookTicker` (the best bid
and ask with their sizes), `NormalizedOrderSize`, `AccountUpdate` (a **delta**, not a snapshot, built from `PositionChange` and
`BalanceChange`, which carry only what the event delivers; `Timestamp` is when the exchange pushed the event and
`TransactionTime` is the matching or transaction time when the account actually changed — use the latter for ordering
and staleness — and is `null`, never filled from the event time, when the exchange does not provide it),
`MarginCall` (with `MarginCallPosition`),
`ResyncRequired`, and the conditional order set: `ConditionalOrderRequest`, `ConditionalOrder` (with `IsActivated`
for a trailing stop — two `New` updates may arrive that differ only there, so include it in any de-duplication key),
`ConditionalOrderUpdate`, `ConditionalOrderIdentifier`.

**Interfaces** — `IExchangeInfoProvider` (symbols, server time), `IExchangeClient` (account, positions, orders,
conditional orders), `IMarketDataFeed` (historical klines, live klines, mark prices, and the book), `IUserDataFeed` (orders,
conditional orders, fills, account changes, margin calls, and the signal that local state must be reconciled in
full).

**Error codes** — `TradeErrorCodes` holds the neutral codes (`trade.order_not_found`, `trade.notional_below_min`,
`trade.rate_limited`, …) and `TradeErrors` builds the corresponding `Error` values. Mapping an exchange's own codes
onto this set is the implementation package's job, so that no strategy ever contains `if (code == -2011)`.
Among them, `trade.close_position_conflict` means a whole-position (closePosition) conditional order already exists on
the same side and the new one was definitely rejected and is not on the book (`Conflict`, not transient); callers
should confirm or replace the existing order rather than treat the outcome as unknown.
`trade.outcome_unknown` means the exchange itself reports that the outcome is unknown and the request may have
executed — confirm a state-changing request by its client id instead of re-sending it — as opposed to
`trade.exchange_unavailable`, where it definitely did not. `trade.order_state_in_transition` means the order is mid-way
through a state change at the exchange (a conditional order being triggered, say): the operation did not take effect,
but it is not a definite failure either, so look the order up again shortly.

### A maker order is priced from the book, not from the last price

`BookTicker` carries the best bid and ask with the size resting at each, and `IMarketDataFeed` exposes it twice:
`GetBookTickerAsync(symbol)` for one snapshot and `SubscribeBookTickersAsync(symbols)` for the live stream.

```csharp
var book = await feed.GetBookTickerAsync("BTCUSDT");

if (book.TryGetValue(out var top))
{
    // Post-Only buy: at the bid or below, never across the spread.
    var entryPrice = top.BidPrice - (2 * symbol.TickSize);
}
```

A strategy that must not take liquidity — Post-Only, or Binance's GTX — has to quote from the bid and the ask.
Pricing from the last traded price or from `MarkPriceUpdate.MarkPrice` produces an order that would match on
arrival, and the exchange rejects it outright. Nothing looks broken: the strategy runs, signals fire, and no
position is ever opened.

Two things the type insists on:

* `ReceivedAt` is **required**. A book starts going stale the moment it leaves the exchange, and a stale one is
  indistinguishable from a fresh one without a timestamp. Defaulting it would put every book in the year 1, so a
  freshness check would silently reject all of them.
* `ExchangeTime` is `null` when the exchange sends none, never a copy of `ReceivedAt`. The gap between the two is
  the transit latency, and filling one from the other makes that measurement permanently zero.

`MidPrice` is for valuation, not for quoting: a buy at the midpoint sits above the best bid, which for a maker
order is a rejection waiting to happen. A negative `Spread` means a crossed book — bad data rather than a
tradable market — and such a book should be discarded rather than priced from.

### Conditional orders travel their own path

Stops, take-profits, and trailing stops go through `PlaceConditionalOrderAsync`, not `PlaceOrderAsync` — exchanges
have moved them onto a separate service with their own numbering, cancellation endpoint, and state machine, and
the plain order endpoint rejects those types outright.

Three consequences that are easy to miss:

* `GetOpenOrdersAsync` does not see them. Reconciling with it alone concludes "no resting orders" while the stops
  sit safely on the other path — or are genuinely missing, which looks identical.
* `SubscribeOrderUpdatesAsync` does not carry them. A stop triggering is visible only on
  `SubscribeConditionalOrderUpdatesAsync`; what reaches the order stream is the fill of the order the trigger
  produced, linked back only through `ConditionalOrder.TriggeredOrderId`.
* `CancelAllOrdersAsync` does not cancel them. An emergency exit calls `CancelAllConditionalOrdersAsync` too,
  because a stop left behind after the position closes opens a new one in the opposite direction.

### Streams are `IAsyncEnumerable<Result<T>>`

Subscriptions return `IAsyncEnumerable<Result<T>>` rather than events:

* a backtest implementation is just a `yield return` over stored candles;
* `await foreach` is sequential, so a strategy is never re-entered with the next candle while it is still handling
  the previous one;
* cancellation and cleanup ride on the `CancellationToken` instead of an easily forgotten `-=`.

Elements are `Result<T>` because reconnects and unparsable messages are everyday events on a live stream, and the
consumer should decide whether to log and continue rather than have an exception tear out of the loop.

## Related packages

* `Ozakboy.Core.Abstractions` — `Result`, `Money`, `Precision`, `RetryPolicy`.
* `Ozakboy.TradeKit.Binance` — the Binance USDⓈ-M implementation of these interfaces.

## License

MIT. See [LICENSE](LICENSE).
