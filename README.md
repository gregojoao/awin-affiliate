# Awin.Affiliate

> A lightweight, strongly typed .NET SDK for the Awin Publisher API — affiliate link generation and reports.

[![CI](https://github.com/gregojoao/awin-affiliate/actions/workflows/ci.yml/badge.svg)](https://github.com/gregojoao/awin-affiliate/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Awin.Affiliate.svg)](https://www.nuget.org/packages/Awin.Affiliate)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%2010-512BD4.svg)](https://dotnet.microsoft.com/)

Small .NET SDK for the Awin Publisher API.

Maintained by **GrecoLabs**.

`Awin.Affiliate` helps affiliate bots, deal monitors, and revenue dashboards build Awin tracking deep links and pull reports (transactions, sales summary, click stats) from the Awin Publisher API.

## What It Does

| Capability | Description |
|---|---|
| Affiliate links | Builds Awin tracking deep links (`cread.php?awinmid=...&awinaffid=...&ued=...`) entirely client-side. |
| Sub-id tagging | Appends up to two click references (`clickref`, `clickref2`) per Awin's spec. |
| Short URL resolving | Optionally follows redirects on the input URL before building the affiliate link. |
| Transactions | Lists conversions from `GET /publishers/{publisherId}/transactions/`. |
| Sales summary | Aggregates a period's transactions into gross revenue, commission, conversion counts, and a top-advertiser breakdown. |
| Click stats | Reads click/impression totals from the Awin creative report (when accessible). |
| Generated link usage | Pulls per-link click/impression/conversion stats from the creative report. |
| Advertisers | Lists every programme the publisher has access to via `GET /publishers/{publisherId}/programmes/`. |

## Installation

After the package is published to NuGet:

```bash
dotnet add package Awin.Affiliate
```

To build from source:

```bash
git clone https://github.com/gregojoao/awin-affiliate.git
cd awin-affiliate
dotnet restore
dotnet test
dotnet pack -c Release
```

The package is generated at:

```text
src/Awin.Affiliate/bin/Release/Awin.Affiliate.<version>.nupkg
```

## Quick Start

```csharp
using Awin.Affiliate.Application;

var options = new AwinAffiliateOptions
{
    PublisherId = Environment.GetEnvironmentVariable("AWIN_PUBLISHER_ID")!
};

using var httpClient = new HttpClient();
var client = new AwinAffiliateClient(httpClient, options);

var result = await client.GenerateAffiliateLinkAsync(new AwinAffiliateLinkRequest
{
    OriginUrl = new Uri("https://www.kabum.com.br/produto/123"),
    AdvertiserId = "12345",                       // Awin advertiser id (a.k.a. awinmid)
    SubIds = new[] { "telegram", "promo-summer" } // clickref / clickref2
});

Console.WriteLine(result.AffiliateUrl);
Console.WriteLine(result.Source);     // TrackingDeepLink
Console.WriteLine(result.OriginUrl);
```

## Configuration

You can pass credentials manually:

```csharp
using Awin.Affiliate.Application;

var options = new AwinAffiliateOptions
{
    PublisherId = "your-publisher-id",
    Endpoint = AwinAffiliateOptions.DefaultEndpoint,
    TrackingEndpoint = AwinAffiliateOptions.DefaultTrackingEndpoint,
    Timeout = TimeSpan.FromSeconds(30)
};
```

For ASP.NET Core, Worker Services, or any app using `Microsoft.Extensions.DependencyInjection`, register the SDK once:

```csharp
using Awin.Affiliate.Infrastructure;

builder.Services.AddAwinAffiliate(builder.Configuration);
```

Then configure secrets through environment variables, user secrets, Key Vault, or any other configuration provider:

```json
{
  "Awin": {
    "Affiliate": {
      "PublisherId": "your-publisher-id",
      "Timeout": "00:00:30"
    }
  }
}
```

In production, prefer environment variables or a secret manager instead of committing secrets to `appsettings.json`.

After registration, inject the client:

```csharp
using Awin.Affiliate.Application;

public sealed class DealPublisher(IAwinAffiliateClient awin)
{
    public async Task PublishAsync(string productUrl, string advertiserId)
    {
        var result = await awin.GenerateAffiliateLinkAsync(new AwinAffiliateLinkRequest
        {
            OriginUrl = new Uri(productUrl),
            AdvertiserId = advertiserId
        });

        Console.WriteLine(result.AffiliateUrl);
    }
}
```

You can also configure options directly in code:

```csharp
using Awin.Affiliate.Infrastructure;

builder.Services.AddAwinAffiliate(options =>
{
    options.PublisherId = builder.Configuration["AWIN_PUBLISHER_ID"]!;
});
```

| Option | Default | Purpose |
|---|---:|---|
| `Endpoint` | `https://api.awin.com` | Awin Publisher API endpoint (used by short-URL resolution). |
| `TrackingEndpoint` | `https://www.awin1.com/cread.php` | cread.php tracking endpoint used to build deep links. |
| `PublisherId` | Empty | Awin publisher id (your awinaffid). Required. |
| `AccessToken` | Empty | OAuth2 long-lived access token. Only required for Reports calls. |
| `Timeout` | `00:00:30` | HTTP request timeout. |

Per-call behavior lives on request objects:

| Request Property | Default | Purpose |
|---|---:|---|
| `AwinAffiliateLinkRequest.OriginUrl` | required | Destination URL the affiliate link will redirect to. |
| `AwinAffiliateLinkRequest.AdvertiserId` | required | Awin advertiser id (`awinmid`). Without it the link does not track. |
| `AwinAffiliateLinkRequest.SubIds` | Empty | Optional tracking sub-ids (mapped to `clickref` / `clickref2`). Awin honours up to two. |
| `AwinAffiliateLinkRequest.ResolveShortUrls` | `false` | Follows redirects on the origin URL before building the affiliate link. |

## Main APIs

Use `IAwinAffiliateClient` when credentials are registered through DI. Use `AwinAffiliateClient` directly when you want to provide `AwinAffiliateOptions` in code.

### `GenerateAffiliateLinkAsync`

Builds an Awin tracking deep link. No HTTP call is made unless `ResolveShortUrls = true`.

```csharp
var result = await client.GenerateAffiliateLinkAsync(new AwinAffiliateLinkRequest
{
    OriginUrl = new Uri(productUrl),
    AdvertiserId = "12345",
    SubIds = new[] { "telegram" }
});

// result.AffiliateUrl =
// https://www.awin1.com/cread.php?awinmid=12345&awinaffid=987654&ued=<encoded>&clickref=telegram
```

### `ResolveAwinUrlAsync`

Follows redirects for a short URL and returns the final destination. Returns the input URL unchanged on network failure.

```csharp
Uri resolved = await client.ResolveAwinUrlAsync(new Uri(shortUrl));
```

## Affiliate reports

Reports live in `Awin.Affiliate.Reports.Application`. They have a separate options type and DI extension so callers that only need link generation don't have to provide an access token.

```csharp
using Awin.Affiliate.Reports.Configuration;
using Awin.Affiliate.Reports.Application;

var options = new AwinAffiliateReportsOptions
{
    PublisherId = Environment.GetEnvironmentVariable("AWIN_PUBLISHER_ID")!,
    AccessToken = Environment.GetEnvironmentVariable("AWIN_ACCESS_TOKEN")!,
};

using var httpClient = new HttpClient();
IAwinAffiliateReportsClient reports = new AwinAffiliateReportsClient(httpClient, options);
```

Or with DI:

```csharp
using Awin.Affiliate.Reports.DependencyInjection;

builder.Services.AddAwinAffiliateReports(builder.Configuration);
```

Reports configuration section:

```json
{
  "Awin": {
    "Reports": {
      "PublisherId": "987654",
      "AccessToken": "your-oauth2-token",
      "DateType": "transaction",
      "Timezone": "Europe/London"
    }
  }
}
```

### `ListConversionsAsync`

```csharp
using Awin.Affiliate.Reports.Application.Requests;

var page = await reports.ListConversionsAsync(new ListAwinConversionsRequest
{
    PeriodStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
    PeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow),
    Status = AwinConversionStatusFilter.Approved,
    AdvertiserIds = new[] { "12345" } // optional
});

foreach (var tx in page.Items)
{
    Console.WriteLine($"{tx.AdvertiserName} — {tx.CommissionAmount} ({tx.Status})");
}
```

### `GetSalesSummaryAsync`

```csharp
var summary = await reports.GetSalesSummaryAsync(new AwinSalesSummaryRequest
{
    PeriodStart = new DateOnly(2026, 5, 1),
    PeriodEnd = new DateOnly(2026, 5, 31)
});

Console.WriteLine($"Conversions: {summary.Conversions}");
Console.WriteLine($"Gross:       {summary.GrossRevenue}");
Console.WriteLine($"Commission:  {summary.Commission}");
Console.WriteLine($"Avg rate:    {summary.AvgCommissionRate:F2}%");
foreach (var advertiser in summary.TopAdvertisers)
{
    Console.WriteLine($"  {advertiser.AdvertiserName}: {advertiser.Commission}");
}
```

`AwinSalesSummary` carries:

| Property | Description |
|---|---|
| `PeriodStart` / `PeriodEnd` | Reporting window. |
| `Conversions` | Number of conversions. |
| `Clicks` | Total clicks, when available. |
| `GrossRevenue` | Sum of sale amounts. |
| `Commission` | Sum of commissionable amounts. |
| `AvgCommissionRate` | `commission / gross * 100`. Zero when gross is zero. |
| `ConversionRate` | `conversions / clicks` (when clicks are available). |
| `ByStatus` | Conversion counts grouped by status. |
| `TopAdvertisers` | Per-advertiser commission breakdown. |
| `Supported` | `false` when the SDK had to degrade (e.g. no access to the report endpoint). |
| `UnsupportedReason` | Why the result is degraded. |

### `GetConversionAsync`

Searches the last 31 days for a transaction matching the given order reference; throws `AwinAffiliateNotFoundException` when nothing matches.

```csharp
var tx = await reports.GetConversionAsync("ORDER-42");
```

### `ListAdvertisersAsync`

Returns every programme (advertiser) accessible to the configured publisher.

```csharp
var advertisers = await reports.ListAdvertisersAsync();
```

### `GetClickStatsAsync` and `GetGeneratedLinkUsageAsync`

Both calls back the Awin creative-report endpoint (`/publishers/{id}/reports/creative/`). When the account lacks access to that endpoint the SDK returns a result with `Supported = false` and a populated `UnsupportedReason` instead of throwing.

```csharp
var clicks = await reports.GetClickStatsAsync(new AwinClickStatsRequest
{
    PeriodStart = new DateOnly(2026, 5, 1),
    PeriodEnd = new DateOnly(2026, 5, 31)
});

var usage = await reports.GetGeneratedLinkUsageAsync(new AwinGeneratedLinkUsageRequest
{
    PeriodStart = new DateOnly(2026, 5, 1),
    PeriodEnd = new DateOnly(2026, 5, 31),
    LinkId = "creative-id-or-link-id"
});
```

## Methods and exceptions

| Method | Awin endpoint | May throw |
|---|---|---|
| `GenerateAffiliateLinkAsync` | none (client-side build) | `ArgumentException` on missing `AdvertiserId` / invalid URL, `InvalidOperationException` on missing options. |
| `ResolveAwinUrlAsync` | (HTTP GET on origin URL) | `AwinAffiliateApiException` on timeout. Returns the input URL on other failures. |
| `ListConversionsAsync` | `GET /publishers/{publisherId}/transactions/` | `AwinAffiliateAuthException` (401/403), `AwinAffiliateRateLimitException` (429), `AwinAffiliateApiException` (4xx/5xx). |
| `GetConversionAsync` | `GET /publishers/{publisherId}/transactions/` (filtered locally) | `AwinAffiliateNotFoundException` when nothing matches. Plus the same HTTP exceptions as above. |
| `GetSalesSummaryAsync` | `GET /publishers/{publisherId}/transactions/` | Same as `ListConversionsAsync`. |
| `GetClickStatsAsync` | `GET /publishers/{publisherId}/reports/creative/` | Degrades to `Supported = false` on 401/403/404. |
| `GetGeneratedLinkUsageAsync` | `GET /publishers/{publisherId}/reports/creative/` | Same as `GetClickStatsAsync`. |
| `ListAdvertisersAsync` | `GET /publishers/{publisherId}/programmes/` | Standard HTTP exceptions. |

Exception hierarchy:

```text
AwinAffiliateException
├── AwinAffiliateApiException       (4xx/5xx, malformed body)
├── AwinAffiliateAuthException      (401/403)
├── AwinAffiliateRateLimitException (429)
├── AwinAffiliateNotFoundException  (missing resource)
└── AwinAffiliateUnsupportedException (feature not available)
```

## Limits and conventions

- **31-day window**: Awin's `transactions/` and `reports/aggregated/` endpoints reject windows larger than 31 days. The SDK validates this client-side and throws `ArgumentException` early.
- **Time zone**: defaults to `Europe/London` (Awin's account default). Override via `AwinAffiliateReportsOptions.Timezone`.
- **Token lifetime**: OAuth2 tokens issued via `Toolbox > API credentials` are long-lived and do not need refresh. The SDK simply sends them as `Authorization: Bearer {token}`.
- **Rate limit**: Awin documents a soft limit around 20 requests/second per publisher. The transport retries once on transient 5xx and timeout errors with exponential backoff; 429 responses surface as `AwinAffiliateRateLimitException` (no implicit retry — back off and retry yourself).
- **Sub-ids**: Awin honours `clickref` and `clickref2`. Sub-ids beyond the first two are silently dropped.
- **Currency**: every monetary value flows through the `Money` value object (`{ Amount, Currency }`). Aggregations refuse to combine mismatched currencies.

## Authentication

1. Sign in to <https://ui.awin.com>.
2. Open **Toolbox > API credentials**.
3. Click **Create OAuth2 token**.
4. Store the token in a secret manager and expose it as `AwinAffiliateReportsOptions.AccessToken`.

The link client (`AwinAffiliateClient`) does not need a token — Awin tracking deep links are built client-side.

## Architecture

The SDK is organized with a small DDD-inspired structure:

| Layer | Responsibility |
|---|---|
| `Domain` | Value objects (`Money`, `AwinAdvertiserIdentity`, `AwinPublisherIdentity`, `AwinTransactionStatus`). |
| `Application` | Public use cases and service abstractions (`IAwinAffiliateClient`, request/result records, options). |
| `Infrastructure` | HTTP transport, link builder, response mappers, exception hierarchy, DI registration. |
| `Reports` | Reports-specific surface (`IAwinAffiliateReportsClient`, requests, options, transport, mappers) — kept in a sub-namespace so callers that only need link generation can ignore it. |

Public namespaces follow the physical project structure:

- `Awin.Affiliate.Application` — link client, options, requests, results
- `Awin.Affiliate.Domain` — value objects
- `Awin.Affiliate.Infrastructure` — DI registration, exceptions, defaults
- `Awin.Affiliate.Reports.Application` — reports client, requests
- `Awin.Affiliate.Reports.Configuration` — reports options
- `Awin.Affiliate.Reports.Domain` — typed reports models
- `Awin.Affiliate.Reports.DependencyInjection` — `AddAwinAffiliateReports`

## Returned Data

`AwinAffiliateLinkResult` contains:

| Property | Description |
|---|---|
| `AffiliateUrl` | Affiliate URL ready to share. |
| `OriginUrl` | Destination URL (after redirect resolution, when requested). |
| `AdvertiserId` | Advertiser id used to build the link. |
| `Source` | `TrackingDeepLink` (always for this release; `ApiConverted` reserved). |

`AwinConversion` contains:

| Property | Description |
|---|---|
| `Id` | Awin transaction id. |
| `AdvertiserId` / `AdvertiserName` | Programme that drove the conversion. |
| `SaleAmount` | Sale gross amount. |
| `CommissionAmount` | Commission payable. |
| `Status` | Typed `AwinTransactionStatus` (Approved / Pending / Declined / Unknown). |
| `TransactionDate` | When the sale (or click) was recorded. |
| `OrderReference` | Advertiser-supplied order reference (`orderRef`). |
| `ClickRef` / `ClickRef2` | Sub-ids recorded by Awin (your `clickref` / `clickref2`). |

## Supported URL formats

The SDK accepts any HTTP/HTTPS URL as the affiliate destination. With `ResolveShortUrls = true`, the SDK first follows redirects on the input — handy for short URLs such as `https://s.click.partner.test/abc`.

```csharp
var resolved = await client.ResolveAwinUrlAsync(new Uri("https://s.click.test/abc"));
```

The cread.php link itself works on every Awin endpoint that uses the `awin1.com` tracking infrastructure (`www.awin1.com`, `awin.com`, regional aliases).

## Development

```bash
dotnet restore
dotnet test
dotnet pack -c Release
```

## Publishing

Before publishing a new NuGet version:

1. Update `<Version>` and `<PackageReleaseNotes>` in `src/Awin.Affiliate/Awin.Affiliate.csproj`.
2. Run the validation:

```bash
dotnet test
dotnet pack -c Release
```

3. Push the package:

```bash
dotnet nuget push src/Awin.Affiliate/bin/Release/Awin.Affiliate.*.nupkg --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json
```

See [PUBLISHING.md](PUBLISHING.md) for the full release checklist.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
