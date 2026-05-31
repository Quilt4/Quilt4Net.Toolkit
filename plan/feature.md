# Feature: Easy cleanups (config-binding gap + minor logging items)

## Goal
Knock out the small, low-risk Toolkit backlog items in one batch.

## Items
1. **Config-binding gap (Important/Easy).** `AddQuilt4NetContent` / `AddQuilt4NetRemoteConfiguration`
   constructed the options object by cherry-picking only ApiKey + Quilt4NetAddress from bound config,
   silently ignoring HttpTimeout / FailureCacheDuration / Ttl / Application / StaleWhileRevalidate set
   in appsettings. Fix: bind the whole section, then apply the special ApiKey/Address fallback to the
   top-level Quilt4Net:ApiKey / :Quilt4NetAddress. Original precedence (incl. top-level address when no
   subsection) preserved by referencing the nullable bound `config`, not the defaulted object.
2. **Cache-TTL notice (Nice/Easy).** After a successful SetContentAsync, log an informational hint that
   other clients won't see the change until their cache TTL expires. (Was a vague //TODO; a log line is
   the least-surprising interpretation — no API/return-shape change.)
3. **ContentFormat null (Nice/Easy) — already done.** The client already does
   `DefaultValue = contentType == null ? null : ...`. Removed the stale backlog item; no code.
4. **Pass-through stream (Nice) — partial.** Removed the redundant double-read of chunked request
   bodies (buffer once, decode that buffer). The full tee-stream rearchitecture is deferred (real
   streaming-semantics risk; re-filed on the backlog as Hard).

## Out of scope
- Full tee-stream request-body capture (deferred; backlog).
- Deeper address-precedence nuance (a bound subsection's defaulted address still shadows the
  top-level address — pre-existing behaviour, preserved unchanged).

## Acceptance criteria
- Content/RemoteConfiguration options bind every appsettings field; ApiKey/Address fallback preserved.
- Build + tests green; warnings under ratchet.

## Done condition
Criteria met, tests green, backlog updated, user confirms. `plan/` removed at composite-PR close-out.
