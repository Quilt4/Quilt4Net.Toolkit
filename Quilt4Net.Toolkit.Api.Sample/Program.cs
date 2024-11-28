using Quilt4Net.Toolkit;
using Quilt4Net.Toolkit.Api;
using Quilt4Net.Toolkit.Api.Sample.Controllers;
using Quilt4Net.Toolkit.Features.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

//Add AddQuilt4NetApi
builder.AddQuilt4NetApi(o =>
{
    //o.FailReadyWhenDegraded = true;

    o.AddComponent(new Component
    {
        Name = "some-service",
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
    });

    o.AddComponentService<MyComponentService>();
});
builder.Services.AddQuilt4NetHealthClient(o =>
{
    o.HealthAddress = new Uri("https://localhost:7119/api/Health");
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

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.UseQuilt4NetApi();

app.Run();
