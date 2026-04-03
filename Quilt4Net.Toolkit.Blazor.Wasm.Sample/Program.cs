using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Quilt4Net.Toolkit;
using Quilt4Net.Toolkit.Blazor;
using Quilt4Net.Toolkit.Blazor.Wasm.Sample;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddRadzenComponents();

builder.Services.AddQuilt4NetBlazorContent(builder.Configuration, o =>
{
    o.ApiKey = builder.Configuration["Quilt4Net:ApiKey"];
    o.Quilt4NetAddress = builder.Configuration["Quilt4Net:Quilt4NetAddress"] ?? "https://quilt4net.com/";
});

builder.Services.AddQuilt4NetRemoteConfiguration(builder.Configuration);

await builder.Build().RunAsync();
