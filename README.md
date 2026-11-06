# CurrencyExchange

Console app that converts an amount from one currency to another using an ISO currency pair.

Rates come from Danmarks Nationalbank's daily feed. When the feed cannot be reached, the tool uses the
last rates it fetched, or the sample rates from the task description if it has never been online.

## Running

Requires the .NET 10 SDK (or newer). Start `CurrencyExchange.Cli` (F5 in Visual Studio, or
`dotnet run --project src/CurrencyExchange.Cli`) and enter one conversion per line, until you type `exit`:

```
Enter a currency pair and an amount, e.g. EUR/DKK 1. Type 'exit' to quit.
> EUR/DKK 1
1 EUR = 7.4747 DKK
Rates as of 2026-11-05 (source: Nationalbanken)
> xyz/dkk 1
Unknown currency 'XYZ'. Supported currencies: AUD, BRL, CAD, CHF, ...
> EUR/DKK 1.000,50
'1.000,50' is not a valid amount. Use digits with an optional '.' or ',' as decimal separator, e.g. 1.5 or 1,5.
> exit
```

- The currency pair is case insensitive: `EUR/DKK`, `eur/dkk` and `Eur/DkK` are the same.
- The amount accepts either `.` or `,` as decimal separator (`1.5` or `1,5`), independent of the machine culture.
  Thousands separators are not supported, because `1,000` would be ambiguous.
- The result is shown with 4 decimals.
- Invalid input prints an error and the next line is read.

## Tests

```
dotnet test                                                      # everything
dotnet test -- --filter-not-trait "Category=Integration"         # offline only
dotnet test -- --filter-trait "Category=Integration"             # only the live Nationalbanken test
```

Tests use xUnit v3 and NSubstitute. `global.json` switches `dotnet test` to Microsoft Testing Platform,
which xUnit v3 requires on newer SDKs.

## Project structure

```
src/
  CurrencyExchange.Domain          Currency, CurrencyPair, ExchangeRate, ExchangeRateTable, Result<T>
  CurrencyExchange.Application     CurrencyConverter (use case), IExchangeRateProvider (port)
  CurrencyExchange.Infrastructure  Nationalbanken feed, embedded rates, JSON file cache, RateSourceChain
  CurrencyExchange.Cli             Input loop and parsing, output formatting, composition root
tests/
  CurrencyExchange.Tests           One folder per layer, plus Integration
```

Dependencies point inwards:

```
Cli            ──► Application, Infrastructure
Infrastructure ──► Application
Application    ──► Domain
```

Domain and Application have no package dependencies. The use case only knows `IExchangeRateProvider`,
so where the rates come from is decided entirely in Infrastructure and wired up in the CLI.

## Conversion

All rates are quoted as DKK per 100 units of the currency, so DKK works as the pivot currency (DKK itself = 100):

```
result = amount × rate(main) / rate(money)

EUR/USD 1  →  1 × 743.94 / 663.11  =  1.1219
JPY/EUR 1  →  1 × 5.9740 / 743.94  =  0.0080
```

If main and money are the same currency the amount is returned unchanged, but both currencies still have
to be known, so `XYZ/XYZ` is an error.

## Where the rates come from

`RateSourceChain` tries the sources in this order:

1. **Danmarks Nationalbank** live feed. The result is saved to `rates-cache.json` next to the executable
2. **Cache**, i.e. the last rates fetched from Nationalbanken
3. **Embedded sample rates**, the table from the task description compiled into the assembly

A network error, an HTTP error, invalid data or a timeout moves on to the next source and logs a warning.
Pressing Ctrl+C stops the app instead of falling back.

The rates are loaded once, on the first conversion, and reused for the rest of the session. All conversions
in a session therefore use the same rates, and only the first one waits for the network.

The output always shows which source and which date was used, e.g. `(source: Nationalbanken, cached)`.

Nationalbanken was chosen because it is free, needs no API key, and quotes rates exactly like the task
(DKK per 100 units). The feed contains around 30 currencies, so more currencies are available online than offline.

### Configuration

`src/CurrencyExchange.Cli/appsettings.json`:

| Setting | Default | |
|---|---|---|
| `Nationalbanken:Url` | `https://www.nationalbanken.dk/api/currencyratesxml?lang=en` | `lang=en` uses `.` as decimal separator |
| `Nationalbanken:Timeout` | `00:00:10` | |

Settings can be overridden with environment variables, e.g. `Nationalbanken__Url=https://nonexistent.invalid`
to simulate being offline.

## Design decisions

**`decimal` for money.** `double` is binary floating point and cannot represent values like `743.94` or `0.1`
exactly, so errors creep in. `decimal` is base 10 with 28-29 significant digits. It is slower, which does not matter here.
Rates are parsed straight from text into `decimal` and never pass through `double`.

**Round only for display.** Calculations keep full precision. Rounding to 4 decimals happens once, in the
formatter, with `MidpointRounding.AwayFromZero` (`Math.Round` defaults to banker's rounding).

**Results instead of exceptions for expected failures.** Invalid input and unknown currencies return a
`Result<T>` with an error message. Exceptions are kept for things that are actually broken: corrupt embedded
data, a rate of zero, or every rate source failing.

**One consistent rate table.** Providers return a single `ExchangeRateTable` (rates, date and source)
instead of individual rates. Both sides of a cross rate therefore always come from the same source and date.

**Culture independence.** Input and output use the invariant culture. This was not theoretical: the
development machine runs with Danish culture, and the first test run failed on `decimal.Parse("1.1219")`.

**Async and cancellation.** Everything that does I/O is async and takes a `CancellationToken`, from Ctrl+C
in `Program.cs` down to `HttpClient` and file access. HttpClient's own timeout is told apart from user
cancellation by checking whether our token was cancelled.

**Dependency injection.** Uses the .NET Generic Host for configuration, logging and DI. `HttpClient`
comes from `IHttpClientFactory`. Pure functions such as the parsers and the formatter are static classes,
because putting interfaces in front of code with no dependencies adds ceremony without making testing easier.

**Best effort cache.** The cache is only a fallback for when the live feed is down, so the tool always
tries for current rates first. A missing or corrupt cache file is treated as "no cache". Failing to write the
cache logs a warning but does not fail the conversion. Writes go to a temporary file first and are
then renamed, so a crash cannot leave a half-written file behind.

## Possible improvements

- Retry with backoff for the live feed (e.g. `Microsoft.Extensions.Http.Resilience`).
- Rate history in SQLite, which also needs no setup, if historical conversions are needed.
- In a long-running service the session-wide rates would need to be refreshed, e.g. once per banking day,
  and `RateSourceChain` should not be a singleton holding on to one `HttpClient`.
- Keyed services instead of the factory registration for the two `IExchangeRateProvider` implementations.
- Skip the network call when the cached rates are recent enough (Nationalbanken publishes once per banking day).
- `decimal` keeps 28-29 significant digits, so amounts close to its limit convert without overflowing but lose their last digits.
