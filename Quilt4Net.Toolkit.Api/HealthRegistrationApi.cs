namespace Quilt4Net.Toolkit.Api;

public static class HealthRegistrationApi //TODO: Revisit
{
    public static void UseQuilt4NetHealthClient(this WebApplication app)
    {
        app.Services.UseQuilt4NetHealthClient();
    }
}