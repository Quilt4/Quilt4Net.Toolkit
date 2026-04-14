# Feature: shared-toggles-across-applications (Toolkit side)

## Goal
Let Toolkit consumers control how the Application name is sent when requesting toggle values, enabling shared/cross-application toggles or application impersonation.

## Originating branch
develop

## Requested by
Florida (via Requests.md 2026-04-06)

## What was done
- Added `Application` property to `RemoteConfigurationOptions` (parity with `ContentOptions.Application`)
- Wired `_options.Application` into the fallback chain in `MakeCallAsync` and `GetAllAsync`
- Fallback chain: per-call `application` → `_options.Application` → entry assembly name

## Usage
```csharp
builder.AddQuilt4NetRemoteConfiguration(o => {
    o.Application = "";  // always request shared values
});
```
- `null` (default) → entry assembly name (backward compatible)
- `""` → always shared
- `"SomeName"` → impersonate another application

## Related
Server-side matching logic already supported shared (null Application) entries. Server-side auto-create was changed to default to shared in the Server repo (commit `4289792`).
