using Quilt4Net.Toolkit.Api;
using Component = Quilt4Net.Toolkit.Api.Component;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

//Add AddQuilt4Net
builder.AddQuilt4Net(o =>
{
    o.FailReadyWhenDegraded = true;

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
