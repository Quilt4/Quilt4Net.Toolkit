using FluentAssertions;
using Xunit;

namespace Quilt4Net.Toolkit.Api.Tests;

public class Class1
{
    [Fact]
    public void A()
    {
        true.Should().BeTrue();
    }
}