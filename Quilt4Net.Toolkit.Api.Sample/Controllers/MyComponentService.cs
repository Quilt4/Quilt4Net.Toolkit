using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers;

internal class MyComponentService : IComponentService
{
    public IEnumerable<Component> GetComponents()
    {
        yield return new Component
        {
            Name = "some-other-service",
            Essential = true,
            CheckAsync = async _ =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                return new CheckResult
                {
                    Success = true,
                    Message = "Some information"
                };
            }
        };
    }
}