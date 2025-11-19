namespace Quilt4Net.Toolkit.Api;

public static class ContentRegistration
{
    public static void AddQuilt4NetContent(this WebApplicationBuilder builder, Action<ContentOptions> options = null)
    {
        builder.Services.AddQuilt4NetContent(options);
    }
}