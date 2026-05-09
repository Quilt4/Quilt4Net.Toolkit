using System.Text.Json;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// One frame in the parsed stack of an Application Insights exception. Mirrors the
/// fields AI stores under <c>AppExceptions.Details[i].parsedStack[j]</c>.
/// </summary>
public sealed record StackFrame
{
    public int Level { get; init; }
    public string Assembly { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int Line { get; init; }

    /// <summary>True when the frame has no file/line — typically a system or framework frame.</summary>
    public bool HasFileLocation => !string.IsNullOrEmpty(FileName) && Line > 0;
}

/// <summary>
/// Parses the AI exception <c>Details</c> payload (a JSON array of detail objects, each
/// carrying its own <c>parsedStack</c>) into a flat sequence of <see cref="StackFrame"/>s
/// suitable for tabular rendering. Robust to either a pre-deserialized
/// <see cref="JsonElement"/> value or a raw JSON string in the row dictionary.
/// </summary>
public static class StackFrameParser
{
    /// <summary>
    /// Parse stack frames from an AI exception row's <c>Raw</c> dictionary.
    /// Returns empty when <c>Details</c> is missing, empty, or unparseable.
    /// </summary>
    public static IReadOnlyList<StackFrame> Parse(IReadOnlyDictionary<string, object> raw)
    {
        if (raw is null) return [];
        if (!raw.TryGetValue("Details", out var detailsObj) || detailsObj is null) return [];

        if (!TryGetJsonElement(detailsObj, out var detailsEl)) return [];

        var frames = new List<StackFrame>();

        if (detailsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var detail in detailsEl.EnumerateArray())
            {
                AppendFrames(detail, frames);
            }
        }
        else if (detailsEl.ValueKind == JsonValueKind.Object)
        {
            // Some AI rows surface a single detail object rather than an array.
            AppendFrames(detailsEl, frames);
        }

        return frames;
    }

    /// <summary>
    /// Convert a stack frame into a Resharper-friendly <c>FileName:line N</c> reference.
    /// When any of <paramref name="sourcePathRoots"/> appears in the path, everything up
    /// to (and including) that segment is stripped — leaving a leading slash + the path
    /// inside the project (e.g. <c>\Features\Team\UserService.cs:line 24</c>) which
    /// Resharper / Rider can resolve from any open solution containing that file.
    /// </summary>
    public static string ToResharperPath(StackFrame frame, IReadOnlyList<string> sourcePathRoots = null)
    {
        if (frame is null || string.IsNullOrEmpty(frame.FileName)) return string.Empty;

        var path = frame.FileName;
        if (sourcePathRoots is { Count: > 0 })
        {
            foreach (var root in sourcePathRoots)
            {
                if (string.IsNullOrEmpty(root)) continue;

                // Match the root as a complete path segment, slash-bounded on either side
                // (so "Foo" doesn't accidentally match "FooBar"). Try both separators.
                var stripped = TryStrip(path, "\\" + root + "\\")
                               ?? TryStrip(path, "/" + root + "/")
                               ?? TryStrip(path, root + "\\")  // falls back to the start of the path
                               ?? TryStrip(path, root + "/");
                if (stripped != null)
                {
                    path = "\\" + stripped.Replace('/', '\\');
                    break;
                }
            }
        }

        return frame.Line > 0 ? $"{path}:line {frame.Line}" : path;
    }

    private static void AppendFrames(JsonElement detail, List<StackFrame> sink)
    {
        if (detail.ValueKind != JsonValueKind.Object) return;
        if (!detail.TryGetProperty("parsedStack", out var ps) || ps.ValueKind != JsonValueKind.Array) return;

        foreach (var frame in ps.EnumerateArray())
        {
            if (frame.ValueKind != JsonValueKind.Object) continue;

            sink.Add(new StackFrame
            {
                Level = TryGetInt(frame, "level"),
                Assembly = TryGetString(frame, "assembly"),
                Method = TryGetString(frame, "method"),
                FileName = TryGetString(frame, "fileName"),
                Line = TryGetInt(frame, "line"),
            });
        }
    }

    private static bool TryGetJsonElement(object value, out JsonElement element)
    {
        switch (value)
        {
            case JsonElement el:
                element = el;
                return true;
            case string s when !string.IsNullOrWhiteSpace(s):
                try
                {
                    element = JsonDocument.Parse(s).RootElement;
                    return true;
                }
                catch (JsonException)
                {
                    element = default;
                    return false;
                }
            default:
                element = default;
                return false;
        }
    }

    private static int TryGetInt(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i)
            ? i
            : 0;

    private static string TryGetString(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? string.Empty
            : string.Empty;

    private static string TryStrip(string path, string marker)
    {
        var idx = path.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return idx < 0 ? null : path.Substring(idx + marker.Length);
    }
}
