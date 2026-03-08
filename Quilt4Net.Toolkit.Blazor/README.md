# Quilt4Net.Toolkit.Blazor
[![GitHub repo](https://img.shields.io/github/repo-size/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Quilt4/Quilt4Net.Toolkit)

Blazor component library for content management, remote configuration, language selection, and Application Insights log viewing. Built on [Radzen](https://blazor.radzen.com/).

Features can be configured and monitored using [Quilt4Net Web](https://quilt4net.com).

## Get started

Install the NuGet package [Quilt4Net.Toolkit.Blazor](https://www.nuget.org/packages/Quilt4Net.Toolkit.Blazor) and register the service.

```csharp
builder.AddQuilt4NetBlazorContent(o =>
{
    o.ApiKey = "YOUR_API_KEY_HERE";
});
```

Add your API key from [Quilt4Net Web](https://quilt4net.com) in `appsettings.json`.

```json
{
  "Quilt4Net": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
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

Inject `IContentService` to retrieve content programmatically.

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

View and search Application Insights logs with the `LogView` component. Requires [Quilt4Net.Toolkit](https://github.com/Quilt4/Quilt4Net.Toolkit/tree/master/Quilt4Net.Toolkit) Application Insights client to be configured.

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

### Tabs

| Tab | Description |
|-----|-------------|
| **Search** | Free-text search across log entries. |
| **Summary** | Grouped view by error fingerprint with count and last occurrence. |
| **Measure** | Performance measurements with line charts (elapsed time by action). |
| **Count** | Log count statistics with column charts. |
| **Trigger** | Development utility for manually triggering log entries. |

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
| `LogSummaryView` | `Fingerprint` | Fingerprint from the route. |
| | `Params` | Encoded navigation params from the `p` query string. |
| | `DetailPath` | Path to navigate to a detail page. When null, clicking a row opens an inline detail view. |

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

## Configuration

All options can be set via code or `appsettings.json`. Code takes priority.

### ContentOptions

| Property | Default | Description |
|----------|---------|-------------|
| `Application` | Assembly name | Application name. |
| `Quilt4NetAddress` | `"https://quilt4net.com/"` | Quilt4Net server address. |
| `ApiKey` | `null` | API key from [Quilt4Net Web](https://quilt4net.com). |

Configuration path: `Quilt4Net:Content`
