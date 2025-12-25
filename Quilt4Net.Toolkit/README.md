# Quilt4Net Toolkit
[![GitHub repo](https://img.shields.io/github/repo-size/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Quilt4/Quilt4Net.Toolkit)

## Features
- Remote feature toggles using [Quilt4Net Web](https://quilt4net.com).
- Remote configuration using [Quilt4Net Web](https://quilt4net.com).

Client consumer for *Quilt4Net Toolkit Api* and access for *Application Insights*.

## AddQuilt4NetApplicationInsightsClient
Register client for reading Application Insights data.

### ApplicationInsightsOptions
- TenantId
- WorkspaceId
- ClientId
- ClientSecret

## AddQuilt4NetHealthClient
Register client for reading data from the health API.

### HealthClientOptions
- HealthAddress

## AddQuilt4NetContent
Register backend usages of content from Quilt4Net.

### ContentOptions
- Application
- Quilt4NetAddress
- ApiKey

## AddQuilt4NetRemoteConfiguration
Register backend usages of remote configuration and feature toggles from Quilt4Net.
