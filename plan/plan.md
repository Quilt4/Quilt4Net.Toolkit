# Plan: CorrelationId — outbound propagation for consuming apps

Feature branch: `feature/correlation-id` (off `develop`).
Batching approach: lands on `develop`; composite PR to `master` later with other features.

## Decisions (confirmed with user)
- **Primary deliverable = public, consumer-facing propagation** for the *consuming app's own*
  outbound HttpClients (to any internal destination), not just the Toolkit's Quilt4Net.Server
  clients. (Re-scoped per user clarification mid-implementation.)
- **Accessor:** HttpContext.Items["CorrelationId"] → else **null**. No `Activity.Current` fallback
  (single id scheme — only a real Quilt4Net CorrelationId is propagated).
- **Opt-in per HttpClient** via `IHttpClientBuilder.AddQuilt4NetCorrelationId()`. NOT blanket
  auto-attach (sending an internal id to arbitrary third parties is leaky / useless).
- **Approach:** `ICorrelationIdAccessor` + `CorrelationIdHandler : DelegatingHandler`, both
  **public**. Toolkit's own business clients (Content, FeatureToggle, ValueGroup) reuse it via
  IHttpClientFactory — secondary benefit, also removes their per-call `new HttpClient()` socket
  anti-pattern.

## Status
- [x] 0. Plan drafted + re-scoped + decisions confirmed.

## Steps
- [x] 1. Shared constant `CorrelationConstants.HeaderName = "X-Correlation-ID"` (+ `ItemKey`) in
      `Quilt4Net.Toolkit/Framework/`. Repoint `Api.Framework.CorrelationIdMiddleware` at it so
      client + server share one definition. [done in code, pending wire-up]
- [x] 2. `ICorrelationIdAccessor` (**public**) + `CorrelationIdAccessor` (Framework/). `Current` =
      HttpContext.Items["CorrelationId"] via **optional** IHttpContextAccessor, else null.
- [x] 3. `CorrelationIdHandler : DelegatingHandler` (**public**) — if Current non-empty and request
      has no X-Correlation-ID yet, add it. Pass through otherwise.
- [x] 4. **Public registration surface (headline API):**
      - `IServiceCollection.AddQuilt4NetCorrelationId()` — AddHttpContextAccessor(); TryAdd accessor;
        add the handler (transient).
      - `IHttpClientBuilder.AddQuilt4NetCorrelationId()` — `.AddHttpMessageHandler<CorrelationIdHandler>()`
        so a consumer opts a specific client in: `AddHttpClient("internal").AddQuilt4NetCorrelationId()`.
- [x] 5. Migrate the 3 Toolkit business clients to IHttpClientFactory with the handler attached
      (X-API-KEY + BaseAddress configured once); replace `GetHttpClient()` / `new HttpClient()`.
      Registrations: ContentRegistration, RemoteConfigurationRegistration, ValueGroupRegistration.
- [x] 6. Tests: accessor (HttpContext present → id; absent → null); handler adds/skips/passes
      through; IHttpClientBuilder opt-in wires the handler; the 3 clients forward the header
      end-to-end (mock primary handler). Keep existing tests green.
- [x] 7. README: "Correlation across services" — consumer opt-in for their own clients + AI search.
- [x] 8. Build + full test suite; warning count under CI ratchet baseline.

## Notes
- Server-side already honors inbound header + scopes logging + supports AI search — not touched.
- ConnectionService + RemoteApplicationInsightsConfigurationProvider stay on `new HttpClient()`
  for now (infra/health/config, usually outside a request scope) — not in this feature's client set.
- DI lifetimes: the 3 business clients are Singletons created via factory lambdas today. They must
  take `IHttpClientFactory` and call `CreateClient(name)` per call (factory clients are cheap and
  pooled). IHttpContextAccessor is a singleton; reading HttpContext per-call is thread-safe.
- Already created in code (steps 1-2 partial): `CorrelationConstants`, `ICorrelationIdAccessor`.
  These were written as the right foundation before the re-scope and remain valid (made public).