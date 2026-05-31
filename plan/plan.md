# Plan: Mask sensitive headers in request/response logging

Feature branch: `feature/mask-sensitive-headers` (off `develop`).
Batching approach: lands on `develop`; composite PR to `master` later with other features.

## Decisions (confirmed)
- Mask **value** (placeholder `***`), keep the header key — presence visible, value hidden.
- Default sensitive set: Authorization, X-API-KEY, Proxy-Authorization, Cookie, Set-Cookie.
- Default ON (`MaskSensitiveHeaders = true`); consumer can replace the list or opt out.
- Default-path only; a configured `Interceptor` still owns redaction.

## Status
- [x] 0. Plan drafted.

## Steps
- [x] 1. `LoggingOptions`: add `bool MaskSensitiveHeaders = true` and `string[] SensitiveHeaders`
      (default set above), with XML docs + appsettings note.
- [x] 2. `CompiledLoggingOptions`: pre-compile `HashSet<string>` (StringComparer.OrdinalIgnoreCase)
      of sensitive header names + carry the `MaskSensitiveHeaders` flag.
- [x] 3. Middleware default branch: replace the `Cookie`-drop logic with a mask pass over request
      AND response headers — value → `***` when the name is in the set and masking is on. Keep the
      existing empty-value filtering.
- [x] 4. Tests (Api.Tests): default masks Authorization + X-API-KEY (request + response); custom
      list; opt-out logs verbatim; non-sensitive untouched; case-insensitive; key kept / value `***`.
- [x] 5. README (Api): masking default + configuration/opt-out.
- [x] 6. Build + full suite; warning count under CI ratchet baseline (248).

## Notes
- `Mask` placeholder string defined once (const).
- Watch: response masking is new (today only the request dropped Cookie); ensure Set-Cookie is in
  the default set so response secrets are covered too.
- The middleware class is `RequestResponseLoggingMiddleware` (file RequestBodyLoggingMiddleware.cs).