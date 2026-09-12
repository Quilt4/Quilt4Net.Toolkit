namespace Quilt4Net.Toolkit.Features.Content.Pages;

/// <summary>
/// Move a page up one or more stages. Sent to <c>POST Api/ContentPage/promote</c>, which requires
/// the <c>content:write</c> scope.
/// </summary>
/// <remarks>
/// Promotion is the deliberate half of the write model: a push writes the lowest stage, and this
/// is how content reaches the stages above it. It is exposed so an operator's own tooling can
/// drive a release, not so a pipeline can promote its own writes unattended — doing that turns a
/// deploy into a publish and gives back the blast radius the stage model exists to bound.
/// </remarks>
public sealed record ContentPagePromoteRequest
{
    /// <summary>The page to promote, by slug.</summary>
    public required string Slug { get; init; }

    /// <summary>Which language's row to promote. <see cref="Guid.Empty"/> is the default
    /// language.</summary>
    public Guid LanguageKey { get; init; }

    /// <summary>
    /// Stage order to promote to. Must be above the lowest stage — promoting to the stage a write
    /// already lands at is refused rather than silently doing nothing.
    /// </summary>
    public required int TargetStageOrder { get; init; }
}
