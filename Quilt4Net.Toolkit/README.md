# Quilt4Net Toolkit
[![GitHub repo](https://img.shields.io/github/repo-size/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Quilt4/Quilt4Net.Toolkit)

The features can be configured and monitored using [Quilt4Net Web](https://quilt4net.com).
Your need to register an account there to get an ApiKey to be used in your application.

## Feature Toggles and Remote Configuration
Configure your application remotley on [Quilt4Net Web](https://quilt4net.com) or with a component on your site that can be found in [Quilt4Net.Toolkit.Blazor](https://github.com/Quilt4/Quilt4Net.Toolkit/tree/master/Quilt4Net.Toolkit.Blazor).
*Feature Toggles* is really just a boolean values of *Remote Configuration*.

### Get started

Install nuget package `Quilt4Net.Toolkit`

Register *AddQuilt4NetRemoteConfiguration* as a service and use it in the app.
```
var builder = WebApplication.CreateBuilder(args);
...
builder.Services.AddQuilt4NetRemoteConfiguration();

await builder.Build().RunAsync();
```

You have to get an API key from [Quilt4Net](https://quilt4net.com).
The ApiKey can be placed in appsettings.json, in User Secrets or in code for testing.

```
{
  "Quilt4Net": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

To use the feature, inject `IFeatureToggleService` or `IRemoteConfigurationService` and use the services.

## Other features
- Content usage and management.
- Health check client.
- Application insights client.