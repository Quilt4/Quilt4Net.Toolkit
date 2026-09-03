using Quilt4Net.Toolkit.Features.Issue;

namespace Quilt4Net.Toolkit.Blazor.Features.Issue;

/// <summary>
/// Turns a <see cref="RoadmapResponse"/> into placed geometry for the roadmap figure.
/// </summary>
/// <remarks>
/// Layout is computed here rather than in the component's markup so it can be asserted directly:
/// "lane two sits below lane one" and "an edge joins the two boxes it names" are the properties worth
/// testing, and they are invisible from a rendered SVG string.
/// </remarks>
internal static class RoadmapLayout
{
    internal const double GutterWidth = 176;
    internal const double BandWidth = 288;
    internal const double ItemWidth = 256;
    internal const double ItemHeight = 56;
    internal const double ItemGap = 12;
    internal const double LanePadding = 16;
    internal const double HeaderHeight = 36;
    internal const double BandCount = 3;

    /// <summary>Total figure width. Fixed, because the three bands are fixed.</summary>
    internal const double Width = GutterWidth + BandWidth * BandCount;

    public static RoadmapFigure Build(RoadmapResponse roadmap)
    {
        var lanes = new List<RoadmapLane>();
        var placed = new Dictionary<int, PlacedItem>();
        var y = HeaderHeight;

        foreach (var route in roadmap.Routes)
        {
            var bands = new[] { route.Now, route.Next, route.Later };
            var deepest = bands.Max(b => b.Length);
            var laneHeight = Math.Max(ItemHeight + LanePadding * 2, deepest * (ItemHeight + ItemGap) - ItemGap + LanePadding * 2);

            for (var band = 0; band < bands.Length; band++)
            {
                for (var index = 0; index < bands[band].Length; index++)
                {
                    var item = bands[band][index];
                    var x = GutterWidth + band * BandWidth + (BandWidth - ItemWidth) / 2;
                    var itemY = y + LanePadding + index * (ItemHeight + ItemGap);
                    placed[item.Number] = new PlacedItem(item, x, itemY);
                }
            }

            lanes.Add(new RoadmapLane(route.Name, y, laneHeight));
            y += laneHeight;
        }

        var edges = roadmap.Edges
            .Where(e => placed.ContainsKey(e.FromNumber) && placed.ContainsKey(e.ToNumber))
            .Select(e => new PlacedEdge(e, placed[e.FromNumber], placed[e.ToNumber]))
            .ToArray();

        return new RoadmapFigure(lanes.ToArray(), placed.Values.ToArray(), edges, Width, Math.Max(y, HeaderHeight + ItemHeight));
    }
}

/// <summary>The whole figure, laid out.</summary>
internal sealed record RoadmapFigure(RoadmapLane[] Lanes, PlacedItem[] Items, PlacedEdge[] Edges, double Width, double Height);

/// <summary>One lane band, from <paramref name="Top"/> down.</summary>
internal sealed record RoadmapLane(string Name, double Top, double Height);

/// <summary>One issue box at a computed position.</summary>
internal sealed record PlacedItem(RoadmapItemResponse Item, double X, double Y)
{
    public double CenterY => Y + RoadmapLayout.ItemHeight / 2;
    public double Right => X + RoadmapLayout.ItemWidth;
}

/// <summary>One edge joining two placed boxes.</summary>
internal sealed record PlacedEdge(RoadmapEdgeResponse Edge, PlacedItem From, PlacedItem To)
{
    /// <summary>
    /// A cubic bezier leaving the source's right edge and arriving at the target's left edge. When
    /// the target sits left of the source the curve bows outward instead of doubling back through
    /// the boxes between them.
    /// </summary>
    public string Path()
    {
        var x1 = From.Right;
        var y1 = From.CenterY;
        var x2 = To.X;
        var y2 = To.CenterY;
        var span = Math.Max(Math.Abs(x2 - x1), 48);
        var bow = x2 >= x1 ? span * 0.4 : span * 0.6;
        return Format($"M {x1} {y1} C {x1 + bow} {y1}, {x2 - bow} {y2}, {x2} {y2}");
    }

    private static string Format(FormattableString value) =>
        FormattableString.Invariant(value);
}
