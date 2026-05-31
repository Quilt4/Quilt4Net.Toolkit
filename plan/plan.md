# Plan: Configurable StaleWhileRevalidate flag

Feature branch: `feature/stale-while-revalidate-flag` (off `develop`). Batched for composite PR.

## Status
- [x] 1. `StaleWhileRevalidate` (default true) on ContentOptions + RemoteConfigurationOptions.
- [x] 2. Gate the SWR branch in RemoteContentCallService + RemoteConfigCallService.
- [x] 3. Flow `config?.StaleWhileRevalidate` through both registrations.
- [x] 4. Tests (StaleWhileRevalidateTests): default true; disabled→sync fresh; enabled→stale.
- [x] 5. README rows for both options.
- [x] 6. Build + full suite; warnings under ratchet.

## Notes
- Disabled path reuses the existing FetchWithTimeout; the catch still serves stale on error.
- Found a pre-existing gap: Content/RemoteConfiguration registrations only copy ApiKey + Address
  from bound config, silently ignoring HttpTimeout/FailureCacheDuration/Ttl/Application from
  appsettings. Fixed StaleWhileRevalidate here; logged the rest to the backlog.
