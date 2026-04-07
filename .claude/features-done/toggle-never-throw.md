# Feature: toggle-never-throw

## Goal
All read operations in the Toolkit (toggles, content, languages) should never throw — always return a fallback value and log errors.

## Originating branch
develop

## Requested by
Florida (via Requests.md, 2026-04-06)

## What was done
- Removed UnauthorizedAccessException throw on 401 for toggles
- Removed InvalidCastException re-throw for toggles
- GetAllAsync returns empty array on error instead of throwing
- GetLanguagesAsync returns cached or empty array instead of throwing
- All services return stale cached value on failure (last known good)
- Failure cache TTL derived from last successful server response
- FailureCacheDuration default changed from 60 to 10 minutes
- Added Information-level logging: key, elapsed ms, source (Server/Cache/StaleCache/Default), staleness
- Added stale-while-revalidate: stale cache returned immediately, background refresh
- Added configurable HttpTimeout (default 5s) on both RemoteConfigurationOptions and ContentOptions
- 3 integration tests for toggle fallback behavior
