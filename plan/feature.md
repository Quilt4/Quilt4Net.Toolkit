# Feature: Mask sensitive headers in request/response logging

## Goal
Stop the Quilt4Net.Toolkit.Api request/response logger from writing secret-bearing headers
(`Authorization`, `X-API-KEY`, cookies, …) in clear text to Application Insights / logs. Mask
their values by default, with consumer-configurable header names and an opt-out.

## Current behaviour (the gap)
`RequestResponseLoggingMiddleware` captures all headers via `BuildHeaders`. When no `Interceptor`
is configured (the common default), the fallback path:
- drops `Cookie` from the request entirely,
- logs **every other header verbatim**, including `Authorization`, `X-API-KEY`, `Proxy-Authorization`, etc.

So an app using the default logging emits its own/upstream secrets into telemetry.

## Design revision (final)
First cut added a dedicated masking path (`MaskSensitiveHeaders` bool + `SensitiveHeaders` array +
`CompiledLoggingOptions.MaskHeaderValue`). Per review, that duplicated the existing `Interceptor`
hook. **Final design folds masking into `Interceptor`:**
- `Interceptor` now **defaults** to `LoggingOptions.MaskSensitiveHeadersInterceptor` (masks the
  values of `SensitiveHeaders`, request + response, case-insensitive, key kept / value `***`).
- `Interceptor = null` → log verbatim (the opt-out; no separate bool needed).
- Custom `Interceptor` → full control; can call `MaskSensitiveHeadersInterceptor` to compose masking.
- `SensitiveHeaders` stays (appsettings-configurable, feeds the default interceptor).
- `MaskSensitiveHeaders` bool and the `CompiledLoggingOptions` masking helper are **removed** — no
  second delegate, one redaction hook.

## Design (original — superseded by the revision above)
1. **`LoggingOptions.SensitiveHeaders`** — `string[]` of header names whose values are masked.
   Default set: `Authorization`, `X-API-KEY`, `Proxy-Authorization`, `Cookie`, `Set-Cookie`.
   Case-insensitive. Consumers can replace/extend the list.
2. **`LoggingOptions.MaskSensitiveHeaders`** — `bool`, default `true`. Opt-out escape hatch.
3. **Mask value** — replace with placeholder `"***"` rather than dropping the key, so logs still
   show the header was present (useful for "was an API key sent?") without leaking the value.
   (`Cookie` previously dropped — now masked, so its presence is visible.)
4. **Where:** apply in the default (no-interceptor) masking branch for BOTH request and response
   headers. Pre-compile the name set into `CompiledLoggingOptions` as a case-insensitive HashSet so
   the per-request path is allocation-light, mirroring the existing `IncludePathRegex` pattern.
5. **Interceptor precedence unchanged:** a consumer-supplied `Interceptor` runs instead (they own
   redaction). Masking applies only to the built-in default path — same shape as today's Cookie drop.

## Scope (this feature)
1. `LoggingOptions.SensitiveHeaders` + `MaskSensitiveHeaders` (XML docs + appsettings note).
2. Pre-compiled lookup in `CompiledLoggingOptions`.
3. Apply masking to request + response headers in the default branch of the middleware.
4. Tests: default masks Authorization/X-API-KEY (request + response); custom list; opt-out;
   non-sensitive untouched; case-insensitive; key retained / value `***`.
5. README (Api): document the masking default + how to configure/opt out.

## Out of scope
- Body redaction (the `Interceptor` already covers arbitrary body/secret scrubbing).
- Masking in the Toolkit's outbound clients (they don't log their own request headers).
- Query-string secret masking (possible follow-up).

## Acceptance criteria
- With defaults, `Authorization` and `X-API-KEY` values are `***` in both request and response logs;
  the header key remains present.
- A consumer can set their own `SensitiveHeaders` or disable via `MaskSensitiveHeaders=false`.
- Matching is case-insensitive.
- A configured `Interceptor` still fully controls redaction (masking is default-path only).
- Build + tests pass; warning count under the CI ratchet baseline.

## Done condition
All criteria met, tests green, README updated, user confirms. `plan/` removed at composite-PR
close-out (repo batches features on `develop`).
