using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit;

public interface IComponentService
{
    public IEnumerable<Component> GetComponents();
}