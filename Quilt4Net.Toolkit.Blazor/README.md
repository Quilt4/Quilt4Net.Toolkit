# Quilt4Net.Toolkit.Blazor
[![GitHub repo](https://img.shields.io/github/repo-size/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Quilt4/Quilt4Net.Toolkit)

Blazor component library for content management, remote configuration, language selection, and Application Insights log viewing. Built on [Radzen](https://blazor.radzen.com/).

Features can be configured and monitored using [Quilt4Net Web](https://quilt4net.com).

## Get started

Install the NuGet package [Quilt4Net.Toolkit.Blazor](https://www.nuget.org/packages/Quilt4Net.Toolkit.Blazor) and register the service in `Program.cs`.

```csharp
builder.AddQuilt4NetBlazorContent();
```

Add your API key from [Quilt4Net Web](https://quilt4net.com) in `appsettings.json`.

```json
{
  "Quilt4Net": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

The API key is read automatically from the `Quilt4Net:ApiKey` configuration path. You can also set it explicitly in code:

```csharp
builder.AddQuilt4NetBlazorContent(o =>
{
    o.ApiKey = builder.Configuration["Quilt4Net:ApiKey"];
});
```

## Content components

Render managed content using built-in Blazor components. Each component takes a `Key` to identify the content and a default value used when no content exists on the server.

### Quilt4Text

Renders plain text using a Radzen `TextStyle`.

```razor
<Quilt4Text Key="MyText1" Default="Welcome" TextStyle="TextStyle.H1" />
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Key` | | Content key. |
| `Default` | | Fallback text when no content exists. |
| `TextStyle` | `Body1` | Radzen `TextStyle` (H1, H2, Body1, etc.). |
| `Visible` | `true` | Show or hide the component. |
| `Style` | `null` | Custom CSS styles. |

### Quilt4Content

Renders HTML content. The default value is provided as child content.

```razor
<Quilt4Content Key="MyContent1">
    Some content with <i>formatting</i> and a <a href="https://example.com">link</a>.
</Quilt4Content>
```

| Parameter | Description |
|-----------|-------------|
| `Key` | Content key. |
| `ChildContent` | Default HTML content used when no content exists. |

### Quilt4Span

Renders plain text in a `<span>` tag.

```razor
<Quilt4Span Key="MyLabel" Default="Status:" />
```

### Quilt4Raw

Renders plain text directly without any HTML wrapper.

```razor
<Quilt4Raw Key="MyValue" Default="OK" />
```

### Quilt4Button

Renders a Radzen button with managed text.

```razor
<Quilt4Button TextKey="SubmitBtn" DefaultText="Submit" Icon="send" Click="@OnSubmitClick" />
```

| Parameter | Description |
|-----------|-------------|
| `TextKey` | Content key for the button text. |
| `DefaultText` | Fallback text. |
| `Icon` | Radzen icon name. |
| `Click` | Async click handler (`Func<Task>`). |
| `Style` | Custom CSS styles. |

## Content from code

Inject `IQuilt4ContentService` to retrieve content programmatically. It automatically uses the currently selected language.

```csharp
@inject IQuilt4ContentService ContentService

var text = await ContentService.GetAsync("welcome-message", "Hello!");
```

For advanced scenarios (specific language, HTML format), inject `IContentService` directly:

```csharp
var (value, success) = await _contentService.GetContentAsync("welcome-message", "Hello!", languageKey, ContentFormat.String);
```

## Edit mode

Enable inline editing by setting `Enabled` on `IEditContentService`. When enabled, content components show a pink dotted outline and open an edit dialog on click.

```csharp
@inject IEditContentService EditContentService

<button @onclick="() => EditContentService.Enabled = !EditContentService.Enabled">
    Toggle Edit Mode
</button>
```

Or use the built-in admin component.

```razor
<ContentAdmin />
```

The `ContentAdmin` component provides toggle switches for edit mode and developer mode, plus a button to reload content.

## Languages

Content supports multiple languages managed at [Quilt4Net Web](https://quilt4net.com).

### Language selector

Add the `LanguageSelector` component to let users switch languages.

```razor
<LanguageSelector />
```

The selected language is persisted to browser local storage. All content components update automatically when the language changes.

### Language state

Inject `ILanguageStateService` to manage language state from code.

```csharp
@inject ILanguageStateService LanguageState

// Get available languages
var languages = LanguageState.Languages;

// Change language
LanguageState.Selected = languages.First(l => l.Key == "sv");

// Reload languages from server
await LanguageState.ReloadAsync();
```

### Developer mode

When developer mode is enabled, a special "X" language is added for discovering unmanaged content. Enable it from `ContentAdmin` or from code.

```csharp
LanguageState.DeveloperMode = true;
```

## Environment promotion

Content is loaded by priority from the environment. The environment names and order are configured at [Quilt4Net Web](https://quilt4net.com) (e.g. Development, Test, Production).

- When running locally, default values are inserted into the Development environment.
- Higher environments inherit content from the environment below.
- Content can be promoted between environments (Development to Test, Test to Production).
- Changes made in a higher environment are automatically propagated downstream.
- Texts can be translated manually or by AI.

## Remote configuration

Use the `RemoteConfigurationAdmin` component to view and edit feature toggles and configuration values.

```razor
<RemoteConfigurationAdmin />
```

Set `TogglesOnly="true"` to show only boolean toggles.

```razor
<RemoteConfigurationAdmin TogglesOnly="true" />
```

The component displays a data grid with editors for boolean, integer, and string values. Changes are saved to [Quilt4Net Web](https://quilt4net.com).

## Log viewer

View and search Application Insights logs with the `LogView` component. Requires the Application Insights client to be configured in one of two modes (mutually exclusive):

**Local mode** — credentials live in the consumer's `appsettings.json` under `Quilt4Net:ApplicationInsights`:
```csharp
builder.AddQuilt4NetApplicationInsightsClient();
```

**Remote mode** — credentials are pulled from Quilt4Net.Server using an API key that carries the `monitor:read` scope:
```csharp
builder.AddQuilt4NetBlazorApplicationInsightsClientRemote();
```
The host then stops needing any `Quilt4Net:ApplicationInsights` block; only `Quilt4Net:ApiKey` (or `Quilt4Net:RemoteConfiguration:ApiKey`) is required. When the team has more than one configured workspace on the server, `LogView` renders a built-in configuration **dropdown** (one workspace; selection persists in `localStorage` under `Quilt4Net.Monitor.SelectedConfig.{FilterStorageScope}`) and `VersionMatrixDisplay` renders a **multi-select radio bar** that merges the version matrix across the selected workspaces (selecting none shows all).

```razor
<LogView />
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `ShowRangeSelector` | `true` | Show time range dropdown. |
| `Range` | `null` | Override the time range. |
| `ShowEnvironmentSelector` | `true` | Show environment dropdown. |
| `Environment` | `null` | Override the environment. |
| `Context` | `null` | Application Insights context. |
| `DetailPath` | `null` | Path for the detail page (see below). |
| `SummaryPath` | `null` | Path for the summary page (see below). |
| `Tab` | `null` | Initially selected tab name (e.g. `"summary"`). Typically bound from a `?tab=` query parameter. |
| `OnTabChanged` | — | Fires with the new tab name (lowercase) when the user switches tabs. |
| `ShowTestTab` | `false` | Show the developer-only **Test** tab that triggers logs / exceptions / uncaught throws via `ILogger`. Opt-in so external consumers don't expose log-injection controls accidentally. |
| `ApplicationAliasMap` | `null` | Optional `raw → logical` dictionary applied to every cell that renders an Application name across the embedded tabs. When null, falls back to `ApplicationInsightsOptions.ApplicationAlias`. See **Application alias rendering** below. |
| `FilterStorageScope` | `null` | Optional scope key for browser-`localStorage` persistence of the per-tab filter bar selection. When set, state survives reloads under `Quilt4Net.Log.Filters.{scope}.{tab}`. Hosts that want per-team scoping pass the team key. |
| `SourcePathRoots` | `null` | Project / repository folder names used to shorten file paths on the **Stack Trace** sub-tab in the Detail view. See **Stack Trace + Resharper-friendly file:line** below. |

### Tabs

| Tab | Description |
|-----|-------------|
| **Search** | Free-text search across log entries. Also surfaces a CorrelationId column with click-to-self-search and a copy button — see **CorrelationId column on Search** below. |
| **Summary** | Grouped view by error fingerprint with count and last occurrence. |
| **Measure** | Performance measurements with line charts (elapsed time by action). |
| **Count** | Log count statistics with column charts. |
| **Test** | Opt-in dev utility (set `ShowTestTab="true"`) — triggers traces / exceptions at every `LogLevel`, plus an uncaught throw caught by `Tharga.Blazor.CustomErrorBoundary` so the correlation guid surfaces in `customDimensions["CorrelationId"]` and in the in-page recovery banner. |

### Navigation

By default, clicking a row in the Search, Summary, Measure, or Count tabs opens a Radzen dialog inline. This works out of the box with no additional pages required.

If you want to navigate to dedicated pages instead, set `DetailPath` and `SummaryPath` on `LogView`.

```razor
<LogView DetailPath="/log/detail" SummaryPath="/log/summary" />
```

When paths are set, clicking a row navigates to `{path}/{id}?p={encoded}` where `p` is a URL-safe base64 string containing all navigation parameters.

### Tab deep linking

`LogView` automatically syncs the active tab to the URL as a `?tab=` query parameter and reads it back on load. This enables deep linking and browser back/forward navigation without requiring extra `@page` route declarations.

Bind the `Tab` parameter from a query string on your hosting page:

```razor
@* MyLogPage.razor *@
@page "/log"

<LogView Tab="@Tab" DetailPath="/log/detail" SummaryPath="/log/summary" />

@code {
    [Parameter, SupplyParameterFromQuery]
    public string Tab { get; set; }
}
```

Navigating to `/log?tab=summary` will open the Summary tab directly. Switching tabs updates the URL to `/log?tab=<name>` using `replace: true` so no extra browser history entries are added.

### Page components

Use `LogDetailView` and `LogSummaryView` to build your own dedicated pages that receive the encoded `p` parameter.

```razor
@* MyDetailPage.razor *@
@page "/log/detail/{Id}"
@using Quilt4Net.Toolkit.Blazor.Features.Log

<LogDetailView Id="@Id" Params="@P" SummaryPath="/log/summary" />

@code {
    [Parameter] public string Id { get; set; }
    [Parameter, SupplyParameterFromQuery] public string P { get; set; }
}
```

```razor
@* MySummaryPage.razor *@
@page "/log/summary/{Fingerprint}"
@using Quilt4Net.Toolkit.Blazor.Features.Log

<LogSummaryView Fingerprint="@Fingerprint" Params="@P" DetailPath="/log/detail" />

@code {
    [Parameter] public string Fingerprint { get; set; }
    [Parameter, SupplyParameterFromQuery] public string P { get; set; }
}
```

| Component | Parameter | Description |
|-----------|-----------|-------------|
| `LogDetailView` | `Id` | Log entry ID from the route. |
| | `Params` | Encoded navigation params from the `p` query string. |
| | `SummaryPath` | Path to navigate back to the summary page. When null, the fingerprint is shown as plain text. |
| | `ApplicationAliasMap` | Optional `raw → logical` map (see **Application alias rendering**). |
| | `SourcePathRoots` | Optional folder names that bound where file paths get stripped (see **Stack Trace + Resharper-friendly file:line**). |
| `LogSummaryView` | `Fingerprint` | Fingerprint from the route. |
| | `Params` | Encoded navigation params from the `p` query string. |
| | `DetailPath` | Path to navigate to a detail page. When null, clicking a row opens an inline detail view. |
| | `ApplicationAliasMap` | Same as on `LogDetailView`. |
| | `SourcePathRoots` | Same as on `LogDetailView`. |

### Application alias rendering

The Detail header, the Summary header, and the Application column on the Search / Summary / Count / Measure tabs all render through `<ApplicationName Raw="..." />`. When an `ApplicationAliasMap` is supplied (parameter on `LogView` / `LogDetailView` / `LogSummaryView`, or `ApplicationInsightsOptions.ApplicationAlias` from the core toolkit), each cell shows the logical alias name with the raw `cloud_RoleName` available as a hover tooltip and a dotted underline when they differ. When no map is supplied, the raw name renders as-is.

```razor
<LogView ApplicationAliasMap="@(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["MyApp.Server"] = "myapp-web",
        ["MyApp.Server.Client"] = "myapp-web"
    })" />
```

For per-tenant / per-team alias maps loaded from a database, build the dictionary on the hosting page and pass it. For static maps shared across the whole app, set `ApplicationInsightsOptions.ApplicationAlias` once at startup and the toolkit picks it up automatically (no parameter needed).

### Filter bar (Source / Level / Application / Environment)

The Search and Summary tabs each show a `<LogFilterBar>` above the grid — four `RadzenSelectBar Multi="true"` rows. Source and Level options come from the `LogSource` / `SeverityLevel` enums; Application and Environment options are populated from the loaded data's distinct values (with alias resolution applied to applications). Filtering replaces Radzen's per-column filter inputs.

When `FilterStorageScope` is set on `LogView`, the bar persists each tab's selection to `localStorage` under `Quilt4Net.Log.Filters.{scope}.{tab}` so the user's choices survive page reloads. Pass the team key, the workspace id, or any other identifier that should isolate persistence.

```razor
<LogView FilterStorageScope="@team.Key" />
```

> **Filter scope:** Summary applies the bar's selection client-side to the loaded model (cached). Search currently also applies the bar client-side after the existing server-side `text` / `Range` / `Environment` query — pushing the multi-select into KQL is a possible follow-up for very large result sets.

### CorrelationId column on Search

The Search tab renders a 140 px **Correlation** column showing the first 8 characters of the GUID + `…` in a monospace font, with the full guid available on hover and a `Tharga.Blazor.CopyButton` next to it. Clicking the preview re-runs Search with the full guid as the search text, so "show me every entry sharing this correlation id" is one click.

For the column to be populated, your apps must emit the correlation id as a per-record property. `Quilt4Net.Toolkit.Api`'s `CorrelationIdMiddleware` does this automatically via a logging scope; see the Api package README.

### Stack Trace + Resharper-friendly file:line

The Detail view's **Stack Trace** sub-tab parses `AppExceptions.Details[].parsedStack` into a grid: `# / Assembly / Method / File / Line / Copy`. A "Show frames without file/line" toggle hides system / framework frames by default. Per-row CopyButton produces a `:line N`-suffixed path; when `SourcePathRoots` is supplied, the path is shortened so Resharper / Rider can resolve it from any open solution containing the file:

```razor
<LogView SourcePathRoots="@(new[] { "MyApp.Server" })" />
```

With that root configured, a frame whose `fileName` is `D:\a\1\s\MyApp.Server\Features\Team\UserService.cs` line 24 yields the clipboard text `\Features\Team\UserService.cs:line 24` — paste-ready into the IDE's "Find file" prompt or a chat / ticket. Without `SourcePathRoots` configured, the column shows just the filename and the copy gives the full path.

The Stack Trace tab is exception-only; trace and request rows show a friendly "no parsed stack trace available" alert.

### Breadcrumb integration

`LogView` integrates with `BreadCrumbService` from Tharga.Blazor automatically — it registers the `tab` query parameter as a virtual breadcrumb segment, so the active tab name always appears at the end of the breadcrumb trail without any extra code.

On hosting pages and sub-pages, use `BreadCrumbService` to control which segments are clickable:

```razor
@* Log.razor — hosting page *@
@inject BreadCrumbService BreadCrumbService

protected override Task OnInitializedAsync()
{
    // Remove the link from ancestor segments
    BreadCrumbService.UnlinkSegment("log");
    return base.OnInitializedAsync();
}
```

On sub-pages (summary and detail), relink segments so the breadcrumb points back to the correct tab:

```razor
@* MySummaryPage.razor *@
protected override Task OnInitializedAsync()
{
    BreadCrumbService.UnlinkSegment("log");
    BreadCrumbService.RelinkSegment("summary", "/log?tab=summary");
    return base.OnInitializedAsync();
}
```

On detail pages, remove the URL-path segments for `detail` and the `{Id}` and add virtual breadcrumb segments that reflect the navigation source. The encoded `p` parameter carries a `Reference` value identifying where the user came from:

| Reference | Breadcrumb appended |
|-----------|---------------------|
| `"Search"` | `> Search > {id}` |
| `"Summary"` | `> Summary > {fingerprint} > {id}` |
| `"Measure"` | `> Measure > {id}` |
| `"Count"` | `> Count > {id}` |

```razor
@* MyDetailPage.razor *@
protected override async Task OnInitializedAsync()
{
    BreadCrumbService.UnlinkSegment("log");
    BreadCrumbService.RemoveSegment("detail");
    BreadCrumbService.RemoveSegment(Id);

    var navParams = LogNavParams.Decode(P);
    if (navParams.Reference == "Search")
    {
        BreadCrumbService.AddVirtualSegment("search", "/log?tab=search");
        BreadCrumbService.AddVirtualSegment(Id);
    }
    else if (navParams.Reference == "Summary" && !string.IsNullOrEmpty(navParams.Fingerprint))
    {
        BreadCrumbService.AddVirtualSegment("summary", "/log?tab=summary");
        BreadCrumbService.AddVirtualSegment(navParams.Fingerprint, $"/log/summary/{navParams.Fingerprint}?p={P}");
        BreadCrumbService.AddVirtualSegment(Id);
    }
    else if (navParams.Reference == "Measure")
    {
        BreadCrumbService.AddVirtualSegment("measure", "/log?tab=measure");
        BreadCrumbService.AddVirtualSegment(Id);
    }
    else if (navParams.Reference == "Count")
    {
        BreadCrumbService.AddVirtualSegment("count", "/log?tab=count");
        BreadCrumbService.AddVirtualSegment(Id);
    }

    await base.OnInitializedAsync();
}
```

## Version matrix

`VersionMatrixDisplay` shows the latest version of each application per environment, scanned from Application Insights. It uses the same Application Insights client (local or remote) as `LogView`.

```razor
<VersionMatrixDisplay />
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Context` | `null` | AI context. Leave `null` to use the built-in workspace selector (remote mode) or the configured options (local mode). |
| `Lookback` | toolkit default | How far back to scan for version entries. |
| `EnvironmentOrder` | `ApplicationInsightsOptions.EnvironmentOrder` | Column (environment) ordering. |
| `AliasFolder` | `null` | Optional `raw → logical` folding delegate. When `null`, falls back to `ApplicationInsightsOptions.ApplicationAlias`. |
| `ConfigurationPath` | `null` | When set, an "Edit configuration" link is shown on authentication-failure alerts. |

When the team has more than one configured workspace (remote mode) it renders a built-in **multi-select radio bar**: toggle one or more workspaces and their version data is merged into a single matrix; selecting none shows all. (`LogView` uses a single-select dropdown for the same purpose.)

## Developer monitoring pages

The usual convention is to expose the log and version views on developer-only pages at `/developer/log` and `/developer/version`, gated by a `Developer` role:

```razor
@* Components/Developer/Log.razor *@
@page "/developer/log"
@using Quilt4Net.Toolkit.Blazor.Features.Log
@attribute [Authorize(Roles = "Developer")]

<LogView Tab="@Tab" DetailPath="/developer/log/detail" SummaryPath="/developer/log/summary" ShowTestTab="true" />

@code {
    [Parameter, SupplyParameterFromQuery] public string Tab { get; set; }
}
```

```razor
@* Components/Developer/VersionMatrix.razor *@
@page "/developer/version"
@using Quilt4Net.Toolkit.Blazor.Features.VersionMatrix
@attribute [Authorize(Roles = "Developer")]

<VersionMatrixDisplay />
```

Register the Application Insights client once in `Program.cs`, in **either** local or remote mode (they are mutually exclusive):

**Local** — credentials in `appsettings.json`:
```csharp
builder.AddQuilt4NetApplicationInsightsClient();
```
```json
{
  "Quilt4Net": {
    "ApplicationInsights": {
      "TenantId": "...",
      "WorkspaceId": "...",
      "ClientId": "...",
      "ClientSecret": "..."
    }
  }
}
```

**Remote** — credentials fetched from Quilt4Net.Server with an API key that carries the `monitor:read` scope:
```csharp
builder.AddQuilt4NetBlazorApplicationInsightsClientRemote();
```
```json
{
  "Quilt4Net": {
    "RemoteConfiguration": {
      "Quilt4NetAddress": "https://quilt4net.com/",
      "ApiKey": "<monitor:read API key>"
    }
  }
}
```
Keep the API key out of source control (user-secrets / environment variables). In remote mode the host needs no `Quilt4Net:ApplicationInsights` block. **Configuring both modes is ambiguous** — the remote source wins and the local block is silently ignored, so pick one.

> Use the Blazor `AddQuilt4NetBlazorApplicationInsightsClientRemote()` (not the core `AddQuilt4NetApplicationInsightsClientRemote()`) so the in-component workspace selector and its `localStorage` persistence are wired up.

## Configuration

All options can be set via code or `appsettings.json`. Code takes priority.

### ContentOptions

| Property | Default | Description |
|----------|---------|-------------|
| `Application` | Assembly name | Application name. |
| `Quilt4NetAddress` | `"https://quilt4net.com/"` | Quilt4Net server address. |
| `ApiKey` | `null` | API key from [Quilt4Net Web](https://quilt4net.com). |

Configuration path: `Quilt4Net:Content`
