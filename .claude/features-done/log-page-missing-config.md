# Feature: log-page-missing-config

## Goal
Show an info message when ApplicationInsights configuration is missing instead of crashing the Blazor circuit.

## Originating branch
develop

## Requested by
Florida (via Requests.md, 2026-04-05)

## Acceptance criteria
- [x] LogView shows info message when config is missing/incomplete
- [x] Machine and IpAddress hidden, no circuit crash
- [x] Tests pass (3 bUnit tests)
