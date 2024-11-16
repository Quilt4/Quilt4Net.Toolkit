# Quilt4Net Toolkit Api
[![NuGet](https://img.shields.io/nuget/v/Quilt4Net.Toolkit.Api)](https://www.nuget.org/packages/Quilt4Net.Toolkit.Api)
![Nuget](https://img.shields.io/nuget/dt/Quilt4Net/Quilt4Net.Toolkit.Api)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

[![GitHub repo Issues](https://img.shields.io/github/issues/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Issues)](https://github.com/Quilt4/Quilt4Net.Toolkit/issues?q=is%3Aopen)

### Get started
Register *AddQuilt4Net* as a service and use it in the app.
```
var builder = WebApplication.CreateBuilder(args);

...

builder.Services.AddQuilt4Net();

var app = builder.Build();

...

app.UseQuilt4Net();

app.Run();
```