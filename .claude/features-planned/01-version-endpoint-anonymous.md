# Feature: version-endpoint-anonymous

## Goal
Show version number and environment on the `/api/health/version` endpoint even when the caller is unauthenticated. Sensitive fields (Machine, IpAddress) remain hidden for anonymous users.

## Requested by
Eplicta (via Requests.md, 2026-03-24)

## Scope (Quilt4Net.Toolkit)

### Problem
`ClearDetails(VersionResponse)` in `EndpointHandlerService.cs` nulls out **all** fields (Version, Machine, IpAddress) when `DetailsLevel.AuthenticatedOnly` and the user is not authenticated. Version and Environment are not sensitive and should remain visible.

### Approach
Split `ClearDetails(VersionResponse)` so that it only clears the sensitive fields (`Machine`, `IpAddress`) while keeping `Version`, `Environment`, and `Is64BitProcess` intact.

### Changes
1. **`EndpointHandlerService.ClearDetails(VersionResponse)`** — only null out `Machine` and `IpAddress`, keep `Version`
2. **Tests** — add/update tests verifying:
   - Authenticated user sees all fields
   - Unauthenticated user sees Version + Environment but not Machine/IpAddress
   - `DetailsLevel.NoOne` still clears sensitive fields
3. **README.md** — update the "Environment defaults" table to note that Version is always visible

### Not in scope
- Adding a new `ShowVersionForAnonymous` option (simpler to just always show version)
- Changing behavior for Health/Ready/Metrics endpoints

## Acceptance criteria
- [ ] Unauthenticated calls to `/api/health/version` return Version and Environment
- [ ] Machine and IpAddress are null for unauthenticated calls (when DetailsLevel = AuthenticatedOnly)
- [ ] Authenticated calls still return all fields
- [ ] DetailsLevel.NoOne still hides Machine and IpAddress
- [ ] Tests pass
- [ ] README updated

## Depends on
Nothing

## Risk
Low — only changes what fields are visible to anonymous users on the version endpoint.
