using FluentAssertions;
using Quilt4Net.Toolkit.Blazor.Features.Log;
using Xunit;

namespace Quilt4Net.Toolkit.Blazor.Tests;

public class ApplicationInsightsErrorMessageTests
{
    [Fact]
    public void Format_without_incidentId_omits_incident_suffix()
    {
        var msg = ApplicationInsightsErrorMessage.Format("Could not load.", new InvalidOperationException("boom"));

        msg.Should().Be("Could not load. boom");
        msg.Should().NotContain("[Incident:");
    }

    [Fact]
    public void Format_with_incidentId_appends_bracketed_incident_suffix()
    {
        var msg = ApplicationInsightsErrorMessage.Format("Could not load.", new InvalidOperationException("boom"), "K7XQ4P");

        msg.Should().Be("Could not load. boom [Incident: K7XQ4P]");
    }

    [Fact]
    public void Format_authentication_failure_includes_friendly_text_and_incident()
    {
        var inner = new InvalidOperationException("AADSTS7000215: Invalid client secret provided.");
        var outer = new Exception("wrapper", inner);

        var msg = ApplicationInsightsErrorMessage.Format("Could not load.", outer, "K7XQ4P");

        msg.Should().StartWith("Could not load. Application Insights authentication failed");
        msg.Should().EndWith("[Incident: K7XQ4P]");
    }

    [Fact]
    public void Format_with_null_incidentId_behaves_like_two_arg_overload()
    {
        var msg = ApplicationInsightsErrorMessage.Format("Could not load.", new InvalidOperationException("boom"), null);
        msg.Should().NotContain("[Incident:");
    }

    [Fact]
    public void Format_with_empty_incidentId_behaves_like_two_arg_overload()
    {
        var msg = ApplicationInsightsErrorMessage.Format("Could not load.", new InvalidOperationException("boom"), string.Empty);
        msg.Should().NotContain("[Incident:");
    }
}
