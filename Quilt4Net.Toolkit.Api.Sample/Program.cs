using Quilt4Net.Toolkit.Api;
using Quilt4Net.Toolkit.Api.Features.Health;
using Component = Quilt4Net.Toolkit.Api.Component;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

//Add AddQuilt4Net
builder.AddQuilt4Net(o =>
{
    o.Pattern = "api";

    o.AddComponent(new Component
    {
        Name = "second",
        CheckAsync = async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return HealthStatusResult.Degraded;
        }
    });
    o.AddComponent(new Component
    {
        Name = "fail",
        CheckAsync = _ => throw new InvalidOperationException("Oups")
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseQuilt4Net();

app.Run();
