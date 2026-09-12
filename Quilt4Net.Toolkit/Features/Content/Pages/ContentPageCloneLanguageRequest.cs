namespace Quilt4Net.Toolkit.Features.Content.Pages;

/// <summary>
/// Copy a page into another language, to be translated. Sent to
/// <c>POST Api/ContentPage/clone-language</c>, which requires the <c>content:write</c> scope.
/// </summary>
/// <remarks>
/// The clone starts as a copy of the source language's text, not as an empty page: a translator
/// editing down from the original is working, whereas a translator facing empty fields has to go
/// and find the original first.
/// </remarks>
public sealed record ContentPageCloneLanguageRequest
{
    /// <summary>The page to clone, by slug.</summary>
    public required string Slug { get; init; }

    /// <summary>Language to copy from. <see cref="Guid.Empty"/> is the default language.</summary>
    public Guid SourceLanguageKey { get; init; }

    /// <summary>Language to create. Refused when a row already exists for it, rather than
    /// overwriting a translation somebody has already done.</summary>
    public required Guid TargetLanguageKey { get; init; }
}
