# Plan: Easy cleanups

Feature branch: `feature/easy-cleanups` (off `develop`). Batched for composite PR.

## Status
- [x] 1. Config-binding: bind whole section in ContentRegistration + RemoteConfigurationRegistration;
      keep ApiKey/Address fallback via nullable `config`.
- [x] 2. Cache-TTL informational log after SetContentAsync.
- [x] 3. ContentFormat-null: confirmed already implemented; stale backlog item removed.
- [x] 4. Pass-through stream: removed chunked-body double-read; full tee-stream re-filed (Hard).
- [x] 5. Tests: OptionsBindingTests (all fields bind; address fallback; defaults). 300 green.
- [x] 6. Build + full suite; warnings under ratchet.

## Notes
- Caught + avoided a regression in the binding fix: starting from a fresh options object would have
  let the type-default Quilt4NetAddress shadow the top-level address. Fixed by referencing nullable
  `config` for the two special fields. Covered by Content_falls_back_to_top_level_address test.
