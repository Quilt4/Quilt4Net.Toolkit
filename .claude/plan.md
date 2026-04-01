# Plan: version-endpoint-anonymous

## Steps

- [x] 1. Update `ClearDetails(VersionResponse)` in `EndpointHandlerService.cs` to only null out `Machine` and `IpAddress` (keep `Version`) — removed `Version = null` from the `with` expression
- [x] 2. Add/update tests verifying anonymous vs authenticated vs NoOne behavior — added VersionEndpointTests.cs with 3 tests, all pass
- [x] 3. Update README.md to document that Version is always visible — added note about Version endpoint behavior in DetailsLevel section
- [x] 4. Build and run full test suite — all 65 tests pass across 4 projects
- [~] 5. Commit
