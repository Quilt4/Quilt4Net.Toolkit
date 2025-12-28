//using Quilt4Net.Toolkit.Features.Health;

//namespace Quilt4Net.Toolkit.Api.Tests.Helper;

//internal class OneComponentService : IComponentService
//{
//    private readonly string _name;
//    private readonly bool _success;
//    private readonly bool _essential;
//    private readonly string _message;

//    public OneComponentService(string name, bool success, bool essential, string message)
//    {
//        _name = name;
//        _success = success;
//        _essential = essential;
//        _message = message;
//    }

//    public IEnumerable<Component> GetComponents()
//    {
//        yield return new Component
//        {
//            Name = _name,
//            Essential = _essential,
//            CheckAsync = _ => Task.FromResult(new CheckResult { Success = _success, Message = _message }),
//        };
//    }
//}