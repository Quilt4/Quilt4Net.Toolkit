using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Blazor.Features.Log;
using Xunit;

namespace Quilt4Net.Toolkit.Blazor.Tests;

public class LogViewConfigTests : BunitContext
{
    [Fact]
    public void Shows_Error_When_ApplicationInsights_Not_Configured()
    {
        Services.AddSingleton(Options.Create(new ApplicationInsightsOptions()));

        var cut = Render<LogView>();

        cut.Markup.Should().Contain("Application Insights is not configured");
    }

    [Fact]
    public void Shows_Error_When_ClientSecret_Is_Missing()
    {
        Services.AddSingleton(Options.Create(new ApplicationInsightsOptions
        {
            TenantId = "tenant",
            WorkspaceId = "workspace",
            ClientId = "client",
            ClientSecret = null
        }));

        var cut = Render<LogView>();

        cut.Markup.Should().Contain("Application Insights is not configured");
    }

    [Fact]
    public void Shows_Error_When_Options_Not_Registered()
    {
        var cut = Render<LogView>();

        cut.Markup.Should().Contain("Application Insights is not configured");
    }
}
