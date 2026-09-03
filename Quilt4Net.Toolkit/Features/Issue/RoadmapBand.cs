namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// Where an issue sits on the roadmap's order axis. The band is a <b>suggestion</b> the reader may
/// ignore, which is why it is drawn as a soft background band rather than as an arrow — ordering
/// suggests, while <see cref="IssueLinkKind"/> edges constrain.
/// </summary>
public enum RoadmapBand
{
    /// <summary>Being worked on, or the next thing to pick up.</summary>
    Now,

    /// <summary>Queued behind the current band.</summary>
    Next,

    /// <summary>Wanted, but not scheduled. The default for a new issue.</summary>
    Later
}
