using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// The kind of dependency one issue declares on another. These are the three edge kinds a roadmap
/// may draw; an edge that is none of them is not an edge, and is left off the map.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IssueLinkKind
{
    /// <summary>
    /// The target genuinely cannot start until the source ships. A hard ordering constraint, and
    /// rare — most links people assume are ordered turn out to be <see cref="Cheapens"/>.
    /// Drawn solid. <see cref="Blocks"/> links may not form a cycle.
    /// </summary>
    Blocks,

    /// <summary>
    /// The target is materially cheaper, safer or better-informed once the source has shipped, but
    /// it is not prevented from starting. Drawn dashed. This is the most common real edge.
    /// </summary>
    Cheapens,

    /// <summary>
    /// The two issues touch the same surface, so doing them independently causes rework. Not an
    /// ordering — a warning to give both to one owner. Drawn dotted, and symmetric in meaning.
    /// </summary>
    Overlaps
}
