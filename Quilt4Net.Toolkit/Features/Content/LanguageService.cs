namespace Quilt4Net.Toolkit.Features.Content;

internal class LanguageService : ILanguageService
{
    private readonly IRemoteContentCallService _remoteContentCallService;

    public LanguageService(IRemoteContentCallService remoteContentCallService)
    {
        _remoteContentCallService = remoteContentCallService;
    }

    public Task<Language[]> GetLanguagesAsync()
    {
        return _remoteContentCallService.GetLanguagesAsync();
    }
}