using Quilt4Net.Toolkit.Api;
using Component = Quilt4Net.Toolkit.Api.Component;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

//Add AddQuilt4Net
builder.AddQuilt4Net(o =>
{
    o.Pattern = "api";

    //TODO: Metoden skall returnera...
    //- Tjänsten funkar eller inte
    //- Tjänsten är vesentlig för funktion eller det går att använda utan.

    o.AddComponent(new Component
    {
        Name = "second",
        Essential = true,
        CheckAsync = async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new CheckResult { Success = true };
        }
    });
    o.AddComponent(new Component
    {
        Name = "low-prio-service",
        Essential = false,
        CheckAsync = async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            return new CheckResult { Success = false };
        }
    });
    //o.AddComponent(new Component
    //{
    //    Name = "fail",
    //    Essential = true,
    //    CheckAsync = async _ =>
    //    {
    //        throw new InvalidOperationException("Oups");
    //    }
    //});
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
