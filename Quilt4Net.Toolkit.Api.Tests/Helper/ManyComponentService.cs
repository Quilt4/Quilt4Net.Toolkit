//using Quilt4Net.Toolkit.Features.Health;

//namespace Quilt4Net.Toolkit.Api.Tests.Helper;

//internal class ManyComponentService : IComponentService
//{
//    private readonly string _name;
//    private readonly int _count;
//    private readonly TimeSpan _elapsed;

//    public ManyComponentService(string name, int count, TimeSpan elapsed = default)
//    {
//        _name = name;
//        _count = count;
//        _elapsed = elapsed;
//    }

//    public IEnumerable<Component> GetComponents()
//    {
//        for (var i = 0; i < _count; i++)
//        {
//            yield return new Component
//            {
//                Name = _name,
//                Essential = true,
//                CheckAsync = async s =>
//                {
//                    await Task.Delay(_elapsed);
//                    return new CheckResult { Success = true, Message = "Something" };
//                },
//            };
//        }
//    }
//}