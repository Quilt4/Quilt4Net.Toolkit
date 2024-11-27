using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api;

public interface IComponentService
{
    public IEnumerable<Component> GetComponents();
}