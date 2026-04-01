# Feature: version-endpoint-anonymous

## Goal
Show version number and environment on the `/api/health/version` endpoint even when the caller is unauthenticated. Sensitive fields (Machine, IpAddress) remain hidden for anonymous users.

## Originating branch
develop

## Requested by
Eplicta (via Requests.md, 2026-03-24)

## Acceptance criteria
- [x] Unauthenticated calls to `/api/health/version` return Version and Environment
- [x] Machine and IpAddress are null for unauthenticated calls (when DetailsLevel = AuthenticatedOnly)
- [x] Authenticated calls still return all fields
- [x] DetailsLevel.NoOne still hides Machine and IpAddress
- [x] Tests pass
- [x] README updated
