namespace Quilt4Net.Toolkit.Features.Content;

internal class LanguageService : ILanguageService
{
    public async IAsyncEnumerable<Language> GetLanguagesAsync()
    {
        yield return new Language { Name = "Default" };
    }
}