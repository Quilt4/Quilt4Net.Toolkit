using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Tests.Helper;

internal class ManyComponentService : IComponentService
{
    private readonly string _name;
    private readonly int _count;

    public ManyComponentService(string name, int count)
    {
        _name = name;
        _count = count;
    }

    public IEnumerable<Component> GetComponents()
    {
        for (var i = 0; i < _count; i++)
        {
            yield return new Component
            {
                Name = _name,
                Essential = true,
                CheckAsync = _ => Task.FromResult(new CheckResult { Success = true, Message = "Something" }),
            };
        }
    }
}