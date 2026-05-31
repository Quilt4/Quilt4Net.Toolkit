# Feature: Configurable StaleWhileRevalidate flag

## Goal
Let consumers of the Quilt4Net content + remote-configuration clients choose between
stale-while-revalidate (fast, default) and always-fresh (synchronous refresh) behaviour for
expired cache entries.

## Current behaviour
Both `RemoteContentCallService` and `RemoteConfigCallService` always do stale-while-revalidate:
an expired-but-present cache entry is returned immediately and refreshed in the background. There
is no way to opt out and always wait for a fresh value.

## Design
- Add `bool StaleWhileRevalidate` (default `true`) to `ContentOptions` and `RemoteConfigurationOptions`.
- In both clients, gate the "return stale + background refresh" branch on the flag. When disabled,
  an expired entry falls through to the existing synchronous fetch-with-timeout path (which still
  falls back to any stale value on error via the catch). No-cache behaviour is unchanged.
- Honour the flag from appsettings: the registrations hand-construct the options object, so carry
  `config?.StaleWhileRevalidate` through (see backlog note re: the broader binding gap).

## Scope
1. `StaleWhileRevalidate` on both options (XML docs cross-referencing `HttpTimeout`).
2. Gate the SWR branch in both clients.
3. Flow the flag from config in both registrations.
4. Tests: default true; disabled → synchronous refresh returns fresh value; enabled → stale returned.
5. README rows for both options.

## Out of scope
- Fixing the broader config-binding gap (registrations only copy ApiKey/Address) — logged to backlog.
- ValueGroupClient (separate caching model; its README already documents fresh-by-default).

## Acceptance criteria
- Default unchanged (SWR on). `StaleWhileRevalidate=false` → expired entry refreshed synchronously,
  caller sees the fresh value; on failure still falls back to stale.
- Configurable via code and appsettings.
- Build + tests green; warnings under the CI ratchet.

## Done condition
Criteria met, tests green, README updated, user confirms. `plan/` removed at composite-PR close-out.
