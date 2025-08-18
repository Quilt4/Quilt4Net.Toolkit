namespace Quilt4Net.Toolkit.Features.Content;

public interface ILanguageService
{
    IAsyncEnumerable<Language> GetLanguagesAsync();
}