using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Quilt4Net.Toolkit.Api;

internal class CustomRouteConvention : IApplicationModelConvention
{
    private readonly Quilt4NetApiOptions _options;

    public CustomRouteConvention(Quilt4NetApiOptions options)
    {
        _options = options;
    }

    public void Apply(ApplicationModel application)
    {
        var hc = application.Controllers.FirstOrDefault(x => x.ControllerType.AsType() == typeof(HealthController));
        hc?.Selectors.Clear();
        hc?.Selectors.Add(new SelectorModel
        {
            AttributeRouteModel = new AttributeRouteModel(new RouteAttribute($"{_options.Pattern}{_options.ControllerName}"))
        });

        if (hc != null)
        {
            hc.ApiExplorer.IsVisible = _options.ShowInOpenApi;
        }
    }
}