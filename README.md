# RNIF2

An RNIF 2.0 server written in .NET 8 for educational and demonstration purposes. It implements the **Responder** role of the RosettaNet Implementation Framework (RNIF) version 2.0 — receiving multipart MIME messages over HTTP, validating them, routing them to PIP-specific handlers, and returning Receipt Acknowledgment or Exception Signals.

## What is RNIF 2.0?

RosettaNet Implementation Framework (RNIF) 2.0 is a transport standard for business-to-business (B2B) document exchange. It wraps business documents (PIPs — Partner Interface Processes) in a structured MIME envelope containing four XML parts:

1. **Preamble** — RNIF version, usage code, timestamp
2. **Delivery Header** — sender/receiver identities (DUNS numbers), message tracking ID
3. **Service Header** — PIP code, version, and instance ID
4. **Service Content** — the actual business document (e.g. Purchase Order Request PIP 3A4)

The server responds synchronously with a **Receipt Acknowledgment Signal** (success) or **Exception Signal** (parse/validation failure).

## Project Structure

```
RNIF2/
├── src/
│   ├── RNIF2.Core/          # Models, interfaces, constants, config — zero external deps
│   ├── RNIF2.Parsing/       # MIME parsing via MimeKit, header extractors, message extractor
│   ├── RNIF2.Security/      # S/MIME stub (passthrough; enable via config)
│   ├── RNIF2.Signals/       # Receipt Acknowledgment and Exception Signal builders
│   ├── RNIF2.Handlers/      # Message router and default receipt handler
│   ├── RNIF2.Transport/     # HttpListenerServer (raw HttpListener) and 7-stage request pipeline
│   └── RNIF2.Host/          # Worker.cs (BackgroundService) and DI wiring in Program.cs
└── tests/
    ├── Parsing.Tests/        # 6 tests: MIME parsing and header extraction
    ├── Signals.Tests/        # 6 tests: Receipt Acknowledgment and Exception Signal generation
    └── Integration.Tests/    # 3 tests: full HTTP request/response round-trip
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

## Running the Server

```bash
cd src/Host
dotnet run
```

The server listens at `http://localhost:8080/rnif/` by default. All configuration lives in [src/Host/appsettings.json](src/Host/appsettings.json):

```json
{
  "Rnif": {
    "ListenerPrefix": "http://localhost:8080/rnif/",
    "MaxConcurrentRequests": 20,
    "LocalTradingPartnerId": "urn:duns:123456789",
    "LocalBusinessName": "Acme Corp",
    "MessagePattern": "Async",
    "Security": {
      "EnableSignatureVerification": false,
      "EnableEncryption": false
    }
  }
}
```

| Setting | Description |
|---|---|
| `ListenerPrefix` | HttpListener URL prefix (must end with `/`) |
| `MaxConcurrentRequests` | Backpressure limit via `SemaphoreSlim` |
| `LocalTradingPartnerId` | Your DUNS-based identifier; must match the `ToRole` in incoming messages |
| `MessagePattern` | `Async` or `Sync` — controls signal type in responses |
| `Security.EnableSignatureVerification` | Verify S/MIME signatures (requires a certificate) |
| `Security.EnableEncryption` | Decrypt S/MIME-encrypted payloads |

## Sending a Test Message

A valid RNIF 2.0 message is a `multipart/related` HTTP POST with four XML parts separated by a MIME boundary. Example with `curl`:

```bash
curl -X POST http://localhost:8080/rnif/ \
  -H 'Content-Type: multipart/related; type="application/xml"; boundary="rnif-boundary"' \
  --data-binary @- << 'EOF'
--rnif-boundary
Content-Type: application/xml; charset=utf-8

<?xml version="1.0" encoding="utf-8"?>
<rnif:Preamble xmlns:rnif="urn:rosettanet:specifications:rnif:xsd:02.00">
  <rnif:standardVersion>V02.00</rnif:standardVersion>
  <rnif:GlobalUsageCode>Production</rnif:GlobalUsageCode>
  <rnif:GlobalDateTimeStamp>2024-01-15T10:30:00Z</rnif:GlobalDateTimeStamp>
</rnif:Preamble>
--rnif-boundary
Content-Type: application/xml; charset=utf-8

<?xml version="1.0" encoding="utf-8"?>
<rnif:DeliveryHeader xmlns:rnif="urn:rosettanet:specifications:rnif:xsd:02.00">
  <rnif:FromRole>
    <rnif:PartnerIdentification>
      <rnif:GlobalBusinessIdentifier>urn:duns:987654321</rnif:GlobalBusinessIdentifier>
    </rnif:PartnerIdentification>
  </rnif:FromRole>
  <rnif:ToRole>
    <rnif:PartnerIdentification>
      <rnif:GlobalBusinessIdentifier>urn:duns:123456789</rnif:GlobalBusinessIdentifier>
    </rnif:PartnerIdentification>
  </rnif:ToRole>
  <rnif:MessageTrackingID>uuid:my-message-001</rnif:MessageTrackingID>
  <rnif:MessageDateTime>2024-01-15T10:30:00Z</rnif:MessageDateTime>
</rnif:DeliveryHeader>
--rnif-boundary
Content-Type: application/xml; charset=utf-8

<?xml version="1.0" encoding="utf-8"?>
<rnif:ServiceHeader xmlns:rnif="urn:rosettanet:specifications:rnif:xsd:02.00">
  <rnif:ProcessControl>
    <rnif:PipCode>3A4</rnif:PipCode>
    <rnif:PipVersion>V02.02</rnif:PipVersion>
    <rnif:PipInstanceId>uuid:pip-instance-001</rnif:PipInstanceId>
  </rnif:ProcessControl>
</rnif:ServiceHeader>
--rnif-boundary
Content-Type: application/xml; charset=utf-8

<?xml version="1.0" encoding="utf-8"?>
<PurchaseOrderRequest xmlns="urn:rosettanet:specifications:domain:purchasing:xsd:02.00">
  <requestingOrganization>
    <businessName>My Company</businessName>
  </requestingOrganization>
</PurchaseOrderRequest>
--rnif-boundary--
EOF
```

A successful response returns HTTP 200 with a `ReceiptAcknowledgmentSignal` body referencing the original `MessageTrackingID`. Non-POST requests return HTTP 405.

## Running the Tests

```bash
dotnet test
```

The test suite has 15 tests across three projects:

| Project | Count | What it covers |
|---|---|---|
| `Parsing.Tests` | 6 | MIME part extraction, header parsing (Preamble, DeliveryHeader, ServiceHeader), malformed input |
| `Signals.Tests` | 6 | Receipt Acknowledgment and Exception Signal XML generation |
| `Integration.Tests` | 3 | Full HTTP round-trip: valid message → 200 + ReceiptAck, tracking ID in response, GET → 405 |

The integration tests spin up a real `HttpListenerServer` on a random high port and send live HTTP requests — no mocks.

## Adding a PIP-Specific Handler

To handle a specific PIP code (e.g. `3A4` Purchase Order) differently than the default acknowledgment:

```csharp
// Implement IMessageHandler
public class PurchaseOrderHandler : IMessageHandler
{
    public string PipCode => "3A4"; // exact match; use "*" for catch-all

    public Task<SignalResult> HandleAsync(RnifMessage message, CancellationToken ct)
    {
        // inspect message.ServiceContent, persist, forward, etc.
        return Task.FromResult(SignalResult.Acknowledged());
    }
}

// Register in Program.cs before AddDefaultRnifHandlers()
builder.Services.AddRnifHandler<PurchaseOrderHandler>();
```

The `MessageRouter` resolves handlers by exact PIP code first, then falls back to `"*"` (catch-all), then to the default receipt handler.

## Architecture Notes

- **No ASP.NET** — the transport layer uses raw `System.Net.HttpListener` to stay close to the RNIF spec without framework abstractions.
- **7-stage pipeline** — `RnifRequestPipeline` runs: receive → parse MIME → extract headers → validate → security → route → serialize response.
- **Graceful shutdown** — `HttpListenerServer` drains in-flight requests by acquiring all `SemaphoreSlim` slots before returning from `StopAsync`.
- **`RNIF2.Core` has zero NuGet dependencies** — all shared models, interfaces, and constants are in a dep-free core so downstream projects can reference only what they need.

## Potential Next Steps

- **Initiator role** — implement `IOutboundClient` to send RNIF messages and process incoming Receipt Acknowledgments.
- **S/MIME security** — enable `EnableSignatureVerification` / `EnableEncryption` and wire up a certificate in `SmimeSecurityService`.
- **Persistence** — add a store for duplicate detection (track processed `MessageTrackingID` values).
- **PIP library** — add typed handlers for common PIPs (3A4, 3B2, etc.) with schema validation.
