namespace Quilt4Net.Toolkit.Features.Content;

public interface ILanguageService
{
    Task<Language[]> GetLanguagesAsync(bool forceReload);
}