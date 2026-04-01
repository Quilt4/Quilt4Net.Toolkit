# Feature: version-endpoint-anonymous

## Goal
Show version number and environment on the `/api/health/version` endpoint even when the caller is unauthenticated. Sensitive fields (Machine, IpAddress) remain hidden for anonymous users.

## Originating branch
develop

## Requested by
Eplicta (via Requests.md, 2026-03-24)

## Acceptance criteria
- [ ] Unauthenticated calls to `/api/health/version` return Version and Environment
- [ ] Machine and IpAddress are null for unauthenticated calls (when DetailsLevel = AuthenticatedOnly)
- [ ] Authenticated calls still return all fields
- [ ] DetailsLevel.NoOne still hides Machine and IpAddress
- [ ] Tests pass
- [ ] README updated

## Done condition
User confirms the feature is complete after testing.
