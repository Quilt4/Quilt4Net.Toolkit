using System.Globalization;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quilt4Net.Toolkit.Blazor.Features.Issue;
using Quilt4Net.Toolkit.Features.Issue;
using Quilt4Net.Toolkit.Framework;
using Xunit;

namespace Quilt4Net.Toolkit.Blazor.Tests;

/// <summary>
/// The roadmap has to be a drawing, not a list with arrows described underneath it — so these tests
/// assert the figure: that lanes stack, that items land in the band they belong to, and that each
/// edge kind is drawn differently and carries its reason.
/// </summary>
public class IssueRoadmapTests : BunitContext
{
    private readonly StubIssueService _issueService = new();

    public IssueRoadmapTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IConnectionService>(new AlwaysConnectedService());
        Services.AddSingleton<IIssueService>(_issueService);
    }

    [Fact]
    public void Lanes_stack_downwards_one_per_route()
    {
        var figure = RoadmapLayout.Build(Roadmap(
            Route("content", now: [Item(1)]),
            Route("auth", now: [Item(2)])));

        figure.Lanes.Should().HaveCount(2);
        figure.Lanes[1].Top.Should().BeGreaterThan(figure.Lanes[0].Top);
    }

    [Fact]
    public void A_lane_grows_with_its_deepest_band()
    {
        var shallow = RoadmapLayout.Build(Roadmap(Route("r", now: [Item(1)])));
        var deep = RoadmapLayout.Build(Roadmap(Route("r", now: [Item(1), Item(2), Item(3)])));

        deep.Lanes[0].Height.Should().BeGreaterThan(shallow.Lanes[0].Height);
    }

    [Fact]
    public void Bands_place_items_left_to_right()
    {
        var figure = RoadmapLayout.Build(Roadmap(
            Route("r", now: [Item(1)], next: [Item(2)], later: [Item(3)])));

        var byNumber = figure.Items.ToDictionary(x => x.Item.Number);
        byNumber[1].X.Should().BeLessThan(byNumber[2].X);
        byNumber[2].X.Should().BeLessThan(byNumber[3].X);
    }

    [Fact]
    public void An_edge_joins_the_two_boxes_it_names()
    {
        var figure = RoadmapLayout.Build(Roadmap(
            new[] { Edge(1, 2, IssueLinkKind.Blocks, "schema first") },
            Route("r", now: [Item(1)], next: [Item(2)])));

        figure.Edges.Should().HaveCount(1);
        figure.Edges[0].From.Item.Number.Should().Be(1);
        figure.Edges[0].To.Item.Number.Should().Be(2);
    }

    [Fact]
    public void An_edge_pointing_at_an_unplaced_issue_is_dropped()
    {
        // The endpoint carries no route, so it is in no lane. Drawing a line to nowhere is worse
        // than not drawing it.
        var figure = RoadmapLayout.Build(Roadmap(
            new[] { Edge(1, 99, IssueLinkKind.Blocks, "points off the map") },
            Route("r", now: [Item(1)])));

        figure.Edges.Should().BeEmpty();
    }

    [Fact]
    public void Edge_paths_are_written_in_invariant_culture()
    {
        // On a machine whose culture uses a comma decimal separator, "M 12,5 C …" is not a valid
        // SVG path and the whole figure silently fails to draw.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
            var figure = RoadmapLayout.Build(Roadmap(
                new[] { Edge(1, 2, IssueLinkKind.Cheapens, "cheaper after") },
                Route("r", now: [Item(1)], next: [Item(2)])));

            var path = figure.Edges[0].Path();

            // The bow is a fraction of the span, so this path genuinely has non-integer coordinates —
            // without which the assertion below would hold no matter how the string was built.
            path.Should().MatchRegex(@"\d\.\d", "the sample must contain a decimal, or this test proves nothing");

            // Coordinate pairs are separated by ", ", so a comma followed by a digit can only be a
            // decimal comma. Checking the prefix alone passes even when the later values are wrong.
            path.Should().NotMatchRegex(@",\d");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void The_figure_renders_one_svg_with_a_box_per_item()
    {
        _issueService.Roadmap = Roadmap(Route("content", now: [Item(1)], later: [Item(2)]));

        var cut = Render<IssueRoadmap>();

        cut.FindAll("svg").Should().NotBeEmpty();
        cut.Markup.Should().Contain("#1").And.Contain("#2");
    }

    [Fact]
    public void Each_edge_kind_is_drawn_differently()
    {
        _issueService.Roadmap = Roadmap(
            new[]
            {
                Edge(1, 2, IssueLinkKind.Blocks, "hard"),
                Edge(1, 3, IssueLinkKind.Cheapens, "soft"),
                Edge(2, 3, IssueLinkKind.Overlaps, "same surface")
            },
            Route("r", now: [Item(1)], next: [Item(2)], later: [Item(3)]));

        var cut = Render<IssueRoadmap>();

        var dashes = cut.FindAll("path[stroke-dasharray]")
            .Select(x => x.GetAttribute("stroke-dasharray"))
            .Distinct()
            .ToArray();

        dashes.Should().HaveCountGreaterThanOrEqualTo(3, "each of the three edge kinds must be visually distinct");
    }

    [Fact]
    public void An_edge_states_its_reason_on_the_map()
    {
        _issueService.Roadmap = Roadmap(
            new[] { Edge(1, 2, IssueLinkKind.Blocks, "the schema has to settle first") },
            Route("r", now: [Item(1)], next: [Item(2)]));

        var cut = Render<IssueRoadmap>();

        cut.Markup.Should().Contain("the schema has to settle first");
    }

    [Fact]
    public void An_empty_roadmap_says_so_rather_than_drawing_nothing()
    {
        _issueService.Roadmap = Roadmap();

        var cut = Render<IssueRoadmap>();

        cut.Markup.Should().Contain("Nothing to map yet");
    }

    [Fact]
    public void Unrouted_issues_are_named_rather_than_silently_omitted()
    {
        _issueService.Roadmap = Roadmap() with { UnroutedCount = 4 };

        var cut = Render<IssueRoadmap>();

        cut.Markup.Should().Contain("4 issue(s) carry no route");
    }

    [Fact]
    public void A_failed_load_reports_instead_of_spinning_forever()
    {
        _issueService.Failure = new IssueServiceException("Missing required scope 'issue:read'.", 403);

        var cut = Render<IssueRoadmap>();

        cut.Markup.Should().Contain("could not be loaded");
        cut.Markup.Should().Contain("issue:read");
    }

    private static RoadmapResponse Roadmap(params RoadmapRouteResponse[] routes) => Roadmap([], routes);

    private static RoadmapResponse Roadmap(RoadmapEdgeResponse[] edges, params RoadmapRouteResponse[] routes) => new()
    {
        Routes = routes,
        Edges = edges,
        UnroutedCount = 0,
        HiddenCount = 0,
        GeneratedUtc = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc)
    };

    private static RoadmapRouteResponse Route(string name, RoadmapItemResponse[] now = null, RoadmapItemResponse[] next = null, RoadmapItemResponse[] later = null) => new()
    {
        Name = name,
        Now = now ?? [],
        Next = next ?? [],
        Later = later ?? []
    };

    private static RoadmapItemResponse Item(int number, bool terminal = false, bool quickWin = false) => new()
    {
        Number = number,
        Title = $"issue {number}",
        State = "Todo",
        AssignedUserKey = string.Empty,
        Effort = IssueEffort.S,
        IsTerminal = terminal,
        IsQuickWin = quickWin
    };

    private static RoadmapEdgeResponse Edge(int from, int to, IssueLinkKind kind, string reason) => new()
    {
        FromNumber = from,
        ToNumber = to,
        Kind = kind,
        Reason = reason
    };

    private sealed class AlwaysConnectedService : IConnectionService
    {
        public Task<ConnectionResult> CanConnectAsync(Service service) =>
            Task.FromResult(new ConnectionResult { Success = true, Address = new Uri("https://example.com/") });
    }

    private sealed class StubIssueService : IIssueService
    {
        public RoadmapResponse Roadmap { get; set; }
        public IssueServiceException Failure { get; set; }

        public Task<RoadmapResponse> GetRoadmapAsync(CancellationToken cancellationToken = default) =>
            Failure != null ? throw Failure : Task.FromResult(Roadmap);

        public Task<IssueResponse[]> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<IssueResponse>());
        public Task<IssueResponse> GetAsync(int number, CancellationToken cancellationToken = default) => Task.FromResult<IssueResponse>(null);
        public Task<IssueWorkflowResponse> GetWorkflowAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IssueResponse> CreateAsync(CreateIssueRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IssueResponse> UpdateAsync(int number, UpdateIssueRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IssueResponse> SetStateAsync(int number, SetIssueStateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IssueResponse> AddLinkAsync(int number, AddIssueLinkRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IssueResponse> RemoveLinkAsync(int number, int targetNumber, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(int number, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IssueWorkflowResponse> SetWorkflowAsync(SetIssueWorkflowRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
