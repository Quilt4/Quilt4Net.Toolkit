# Feature: CorrelationId — outbound propagation for consuming apps

## Goal
Quilt4Net.Toolkit's job is to **help the apps that use it** with logging/observability. This
feature gives a consuming app a reusable, opt-in way to attach the current correlation id
(`X-Correlation-ID`) to **its own outbound HTTP calls** — to any internal destination it talks
to, not only to Quilt4Net.Server — so one id spans the whole call chain across services.

> Re-scoped after initial draft (user clarification): the deliverable is a **public,
> consumer-facing** propagation mechanism for the *consumer's* HttpClients, not just internal
> propagation on the Toolkit's own Quilt4Net.Server clients. The Toolkit's own clients reuse the
> same mechanism as a secondary benefit.

## What already exists (do NOT rebuild)
- **Inbound (server):** `Quilt4Net.Toolkit.Api.Framework.CorrelationIdMiddleware` honors/mints
  `X-Correlation-ID`, echoes it, stores it in `HttpContext.Items["CorrelationId"]`, and scopes
  every log line with it.
- **Search:** `IApplicationInsightsService.SearchByCorrelationIdAsync` + the AI log UI.

## The gap (what this feature delivers)
A consuming app handling an inbound request has the id in scope (via the server middleware), but
when it then calls **another service** over HTTP, nothing forwards the id — the next hop mints a
fresh one and the chain breaks. There is no reusable way for the consumer to opt their own
HttpClients into forwarding it.

## Design (public, opt-in)
1. **`ICorrelationIdAccessor` (public)** — `string Current { get; }`. Default `CorrelationIdAccessor`
   reads `HttpContext.Items["CorrelationId"]` via an **optional** `IHttpContextAccessor`; returns
   `null` when there is no ambient id (non-HTTP host, or no scope). No `Activity` fallback
   (confirmed: single id scheme — only a real Quilt4Net correlation id is propagated).
2. **`CorrelationIdHandler : DelegatingHandler` (public)** — on each outbound request, if the
   accessor has a current id and the request doesn't already carry `X-Correlation-ID`, add it.
   Otherwise pass through untouched.
3. **Registration surface (the consumer-facing API):**
   - `services.AddQuilt4NetCorrelationId()` — registers `ICorrelationIdAccessor` (TryAdd) + the
     handler, and ensures `IHttpContextAccessor` is present.
   - `IHttpClientBuilder.AddQuilt4NetCorrelationId()` — opt a specific named/typed HttpClient into
     propagation: `services.AddHttpClient("internal-api").AddQuilt4NetCorrelationId()`.
   - **Opt-in per client by design** — we do NOT blanket-attach to every outbound call. Sending an
     internal correlation header to arbitrary third parties (Fortnox, Worldline, Azure, Stripe…)
     is leaky and useless to services that don't read it. The consumer decorates the clients that
     call *correlation-aware* services (their own internal ones).
4. **Shared header constant** `CorrelationConstants.HeaderName` — one definition referenced by the
   server middleware and the handler so they never drift.
5. **Toolkit's own business clients** (Content, FeatureToggle, ValueGroup) reuse the same handler
   via `IHttpClientFactory` (migrating them off the per-call `new HttpClient()` socket
   anti-pattern). Secondary benefit; same code path as the public one.

## Scope (this feature)
1. `CorrelationConstants`, `ICorrelationIdAccessor` + default impl, `CorrelationIdHandler` (all public).
2. `AddQuilt4NetCorrelationId()` (IServiceCollection) + `AddQuilt4NetCorrelationId()` (IHttpClientBuilder).
3. Migrate the 3 Toolkit business clients to `IHttpClientFactory` with the handler attached.
4. Tests: accessor resolution; handler add/skip/pass-through; IHttpClientBuilder opt-in wiring;
   business clients forward the header end-to-end.
5. README: "Correlation across services" — how a consumer opts their own clients in, and how to
   search by the id.

## Out of scope
- Blanket/global auto-attachment to every outbound HttpClient (rejected — leaky; opt-in instead).
- W3C `traceparent` handling (HttpClient already propagates Activity automatically when OTel is on;
  this feature is the supplementary Quilt4Net human-friendly id).
- Changing the server middleware behavior (already correct).
- Quilt4Net.Server changes — Toolkit-only; Server picks it up on the next package bump.

## Acceptance criteria
- A consumer can register the accessor + handler and opt any of their HttpClients into correlation
  propagation with one call; those clients send `X-Correlation-ID` carrying the ambient id.
- No ambient id / non-HTTP host → no header added, no exceptions.
- Toolkit's own Content/Toggle/ValueGroup clients forward the id to Quilt4Net.Server.
- Header name defined once, shared client+server.
- Build + tests pass; warning count under the CI ratchet baseline.

## Done condition
All acceptance criteria met, tests green, README updated, user confirms. `plan/` removed at the
composite-PR close-out (this repo batches features on `develop`).
