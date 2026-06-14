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

Two shapes ship in this package, with the same purpose — render text managed via Quilt4Net.Server in a Blazor app and have it switch language live without per-component plumbing.

- **Standalone components** (`Quilt4Text`, `Quilt4Content`, `Quilt4Span`, `Quilt4Raw`, `Quilt4Button`, `Quilt4PageTitle`) — content-aware controls. Drop them in where you'd otherwise hard-code a string.
- **Content-aware Radzen wrappers** (`Quilt4Radzen*`) — thin wrappers around Radzen components that resolve a specific `string` attribute (Text / Title / Placeholder / EmptyText / Tooltip) through the content service. Drop them in where the only thing you need to localise on a Radzen control is one or two text attributes.

Every component below:

- Subscribes to `ILanguageStateService.LanguageChangedEvent` so the resolved text re-renders live on language switch.
- Falls back to the supplied default on miss / empty value / lookup failure — never throws.
- Uses the same convention: `{Property}Key` for the content key and `Default{Property}` for the fallback.

### Standalone components

#### Quilt4Text

Renders plain text using a Radzen `TextStyle`.

```razor
<Quilt4Text Key="MyText1" Default="Welcome" TextStyle="TextStyle.H1" />
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Key` | — | Content key. |
| `Default` | — | Fallback text when no content exists. |
| `TextStyle` | `Body1` | Radzen `TextStyle` (H1, H2, Body1, etc.). |
| `Visible` | `true` | Show or hide the component. |
| `Style` | `null` | Custom CSS styles. |

#### Quilt4Content

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

#### Quilt4Span

Renders plain text in a `<span>` tag.

```razor
<Quilt4Span Key="MyLabel" Default="Status:" />
```

#### Quilt4Raw

Renders plain text directly without any HTML wrapper.

```razor
<Quilt4Raw Key="MyValue" Default="OK" />
```

#### Quilt4Button

Renders a Radzen button with managed text — and, optionally, a managed hover tooltip (HTML `title` attribute). Particularly useful for icon-only buttons whose label is the icon alone.

```razor
<Quilt4Button TextKey="SubmitBtn" DefaultText="Submit" Icon="send" Click="@OnSubmitClick" />

<Quilt4Button Icon="delete"
              TooltipKey="btn.delete.tooltip" DefaultTooltip="Delete this row"
              Click="@OnDelete" />
```

| Parameter | Description |
|-----------|-------------|
| `TextKey` | Content key for the button text. |
| `DefaultText` | Fallback text. |
| `Icon` | Radzen icon name. |
| `TooltipKey` | Optional content key resolved into the button's `title` attribute. |
| `DefaultTooltip` | Optional fallback tooltip text. Set without `TooltipKey` for a static, non-localised tooltip. |
| `Click` | Async click handler (`Func<Task>`). |
| `Style` | Custom CSS styles. |

#### Quilt4PageTitle

Wraps Blazor's `<PageTitle>` with a content-aware text.

```razor
<Quilt4PageTitle Key="page.about.title" Default="About us" />
```

| Parameter | Description |
|-----------|-------------|
| `Key` | Content key for the page title. |
| `Default` | Fallback used when the key isn't set or the lookup yields nothing. |

#### Quilt4Tooltip

Wraps arbitrary child content with a `<span title="...">` that pulls the tooltip text from the content service. Use it on any element that doesn't already have a `TooltipKey` parameter (custom controls, links, plain `<div>` elements, etc.).

```razor
<Quilt4Tooltip TooltipKey="status.idle.tooltip" DefaultTooltip="Service is idle">
    <i class="rzi rz-icon-check" />
</Quilt4Tooltip>
```

| Parameter | Description |
|-----------|-------------|
| `TooltipKey` | Content key resolved into the HTML `title` attribute on the wrapping span. |
| `DefaultTooltip` | Fallback tooltip text. |
| `ChildContent` | The element(s) the tooltip is attached to. |
| `Style` | Optional inline style on the wrapping span. |

### Content-aware Radzen wrappers

Drop-in wrappers around Radzen components. Pass the same parameters you would pass to the underlying Radzen control; the wrapper resolves one or two text attributes through the content service and forwards the rest unchanged.

#### Quilt4RadzenAlert

Wraps `<RadzenAlert>`. Resolves `Text` (required) and `Title` (optional) — keep the title slot null for a title-less alert.

```razor
<Quilt4RadzenAlert AlertStyle="AlertStyle.Info"
                   TextKey="alert.permission.body"
                   DefaultText="You don't have permission to do that."
                   TitleKey="alert.permission.title"
                   DefaultTitle="Permission denied" />
```

| Parameter | Description |
|-----------|-------------|
| `TextKey` / `DefaultText` | Content key + fallback for the alert body. |
| `TitleKey` / `DefaultTitle` | Optional title pair. Omit both for a title-less alert. |
| `AlertStyle`, `Shade`, `ShowIcon`, `Icon`, `Variant`, `Size`, `AllowClose`, `Visible`, `Style`, `Close`, `ChildContent` | Pass-through to `<RadzenAlert>`. |

#### Quilt4RadzenDataGridColumn&lt;TItem&gt;

Wraps `<RadzenDataGridColumn>`. Resolves the `Title` attribute. Drop in inline inside a `<RadzenDataGrid>` (or `Quilt4RadzenDataGrid`).

```razor
<RadzenDataGrid TItem="Customer" Data="@_customers">
    <Columns>
        <Quilt4RadzenDataGridColumn TItem="Customer" Property="@nameof(Customer.Name)"
                                    TitleKey="col.customer.name" DefaultTitle="Name" />
    </Columns>
</RadzenDataGrid>
```

| Parameter | Description |
|-----------|-------------|
| `TitleKey` / `DefaultTitle` | Content key + fallback for the column header. |
| `Property`, `Width`, `Sortable`, `Filterable`, `Visible`, `SortOrder`, `TextAlign`, `FormatString`, `Template` | Pass-through to `<RadzenDataGridColumn>`. |

#### Quilt4RadzenDataGrid&lt;TItem&gt;

Wraps `<RadzenDataGrid>`. Resolves the `EmptyText` attribute. Use for plain string empty-states; for richer empty content (icons, buttons), pass an `EmptyTemplate` instead and Radzen picks the template over the resolved text.

```razor
<Quilt4RadzenDataGrid TItem="Customer" Data="@_customers"
                      EmptyTextKey="grid.customers.empty" DefaultEmptyText="No customers yet.">
    <Columns>
        <RadzenDataGridColumn TItem="Customer" Property="@nameof(Customer.Name)" Title="Name" />
    </Columns>
</Quilt4RadzenDataGrid>
```

| Parameter | Description |
|-----------|-------------|
| `EmptyTextKey` / `DefaultEmptyText` | Content key + fallback for the empty-state text. |
| `Data`, `AllowSorting`, `AllowFiltering`, `FilterMode`, `FilterCaseSensitivity`, `AllowPaging`, `PageSize`, `PageSizeOptions`, `ShowPagingSummary`, `PagingSummaryFormat`, `Visible`, `Style`, `Columns`, `EmptyTemplate` | Pass-through to `<RadzenDataGrid>`. |

> Note: this wrapper deliberately forwards only the most-used `RadzenDataGrid` parameters — wider mirroring drifts every Radzen release. If you need finer control, use `<RadzenDataGrid>` directly and place a `<Quilt4Text>` inside its `EmptyTemplate`.

#### Quilt4RadzenPanelMenuItem

Wraps `<RadzenPanelMenuItem>`. Resolves `Text`. Use inside a `<RadzenPanelMenu>`.

```razor
<RadzenPanelMenu>
    <Quilt4RadzenPanelMenuItem TextKey="menu.home" DefaultText="Home" Icon="home" Path="/" />
    <Quilt4RadzenPanelMenuItem TextKey="menu.customers" DefaultText="Customers" Icon="people" Path="/customers" />
</RadzenPanelMenu>
```

| Parameter | Description |
|-----------|-------------|
| `TextKey` / `DefaultText` | Content key + fallback for the menu-item label. |
| `Icon`, `Path`, `Target`, `Expanded`, `ExpandedChanged`, `Selected`, `Disabled`, `IconColor`, `Image`, `ImageAlternateText`, `Click`, `ChildContent` | Pass-through to `<RadzenPanelMenuItem>`. |

#### Quilt4RadzenTabsItem

Wraps `<RadzenTabsItem>`. Resolves `Text`. Use inside `<RadzenTabs>`'s `Tabs` slot.

```razor
<RadzenTabs>
    <Tabs>
        <Quilt4RadzenTabsItem TextKey="tab.overview" DefaultText="Overview">
            ...
        </Quilt4RadzenTabsItem>
    </Tabs>
</RadzenTabs>
```

| Parameter | Description |
|-----------|-------------|
| `TextKey` / `DefaultText` | Content key + fallback for the tab label. |
| `Icon`, `Disabled`, `Visible`, `ChildContent` | Pass-through. |

#### Quilt4RadzenLabel

Wraps `<RadzenLabel>`. Resolves `Text`.

```razor
<Quilt4RadzenLabel TextKey="form.name.label" DefaultText="Name" Component="Name" />
<Quilt4RadzenTextBox Name="Name" @bind-Value="@_name" />
```

| Parameter | Description |
|-----------|-------------|
| `TextKey` / `DefaultText` | Content key + fallback for the label text. |
| `Component` | Name of the associated input — same as `RadzenLabel.Component`. |
| `Visible`, `Style` | Pass-through. |

#### Quilt4RadzenTextBox / Quilt4RadzenTextArea / Quilt4RadzenDropDown&lt;TValue&gt; / Quilt4RadzenNumeric&lt;TValue&gt;

Wrap the four input controls. All resolve `Placeholder` only; the rest is pass-through. Use these whenever the only content-bound attribute is the placeholder — for any richer localisation, drop back to the underlying Radzen control with `<Quilt4Text>` inside as a label.

```razor
<Quilt4RadzenTextBox @bind-Value="@_name"
                     PlaceholderKey="input.name" DefaultPlaceholder="Type your name..." />

<Quilt4RadzenTextArea @bind-Value="@_notes"
                      PlaceholderKey="input.notes" DefaultPlaceholder="Notes..." Rows="4" />

<Quilt4RadzenDropDown TValue="string" Data="@_countries"
                      @bind-Value="@_country"
                      PlaceholderKey="input.country" DefaultPlaceholder="Choose country" />

<Quilt4RadzenNumeric TValue="int" @bind-Value="@_quantity"
                     PlaceholderKey="input.quantity" DefaultPlaceholder="Quantity"
                     Min="1" Max="100" />
```

All four share these content-aware parameters:

| Parameter | Description |
|-----------|-------------|
| `PlaceholderKey` | Content key for the input's placeholder. |
| `DefaultPlaceholder` | Fallback placeholder. |

Plus the type-specific pass-through (`Value`, `ValueChanged`, `Name`, `Disabled`, `ReadOnly`, plus the control-specific ones — `Rows` / `Cols` on TextArea, `Data` / `TextProperty` / `ValueProperty` / `Multiple` / `AllowFiltering` / `AllowClear` on DropDown, `Min` / `Max` / `Step` / `Format` / `ShowUpDown` on Numeric).

### Content-aware Radzen services

For the two Radzen services that take text as a method argument rather than as a component attribute, this package ships content-aware wrappers registered alongside `AddQuilt4NetBlazorContent`:

#### IQuilt4DialogService

Confirm / Alert dialogs whose message and (optional) title come from content keys.

```razor
@inject IQuilt4DialogService Q4Dialogs

@code {
    private async Task DeleteAsync()
    {
        var ok = await Q4Dialogs.ConfirmAsync(
            messageKey: "delete.customer.confirm",
            defaultMessage: "Are you sure you want to delete this customer?",
            titleKey: "delete.confirm.title",
            defaultTitle: "Confirm delete");
        if (ok == true) { ... }
    }
}
```

| Method | Description |
|--------|-------------|
| `ConfirmAsync(messageKey, defaultMessage, titleKey?, defaultTitle?)` | Resolves the keys then calls Radzen's `DialogService.Confirm`. Returns the same `bool?`. |
| `AlertAsync(messageKey, defaultMessage, titleKey?, defaultTitle?)` | Resolves the keys then calls `DialogService.Alert`. |

#### IQuilt4NotificationService

Notifications whose summary and (optional) detail come from content keys.

```razor
@inject IQuilt4NotificationService Q4Notifications

@code {
    private async Task SaveAsync()
    {
        ...
        await Q4Notifications.NotifyAsync(
            NotificationSeverity.Success,
            summaryKey: "save.success.summary",
            defaultSummary: "Saved",
            detailKey: "save.success.detail",
            defaultDetail: "Your changes have been saved.");
    }
}
```

| Method | Description |
|--------|-------------|
| `NotifyAsync(severity, summaryKey, defaultSummary, detailKey?, defaultDetail?, duration = 3000)` | Resolves the keys then posts via `NotificationService.Notify`. |

## Hierarchical pages

Snippets (`Quilt4Content`, `Quilt4Text`, …) are key/value content embedded in a host page's
markup. **Pages** are the full thing — a hierarchical tree of pages, each composed of sections
and typed elements, authored in the Quilt4Net.Server admin. The consumer hosts a single route
and lets the toolkit render whichever page the slug resolves to.

### Reader

Add a catch-all route in your consumer app:

```razor
@page "/pages/{*Slug}"
@using Quilt4Net.Toolkit.Blazor.Features.Content.Pages

<ContentPageView Slug="@Slug" SlugRoutePrefix="/pages/" />

@code {
    [Parameter] public string Slug { get; set; }
}
```

`<ContentPageView>` fetches the page via `IContentPageReader`, renders title + sections, and
pushes the ancestor chain into Tharga.Blazor's `BreadCrumbService` (when registered — silent
no-op otherwise). Section layouts (`OneColumn` / `TwoColumn` / `Hero`) ship with inline-style
fallbacks so it renders sensibly with no stylesheet. Page not found at any stage in the fallback
chain renders a `RadzenAlert` instead of throwing. Element types unknown to the toolkit fall
back to a `<MissingElementPlaceholder>` so a down-version client doesn't blank the page.

Parameters:

| Parameter | Description |
|-----------|-------------|
| `Slug` | Slug to render (required). |
| `SlugRoutePrefix` | Path-prefix breadcrumb links use, e.g. `/pages/`. Default `/`. |
| `NotFoundMessage` | Text shown when no page exists. Default `"Page not found."`. |
| `Application` | Application override forwarded to the reader. Most consumers leave null. |

### Menu

`<ContentMenu>` renders the page tree as a `RadzenPanelMenu`, filtering to `ShowInMenu` pages
and sorted by `Order`. Hosts that already have their own `RadzenPanelMenu` slot the items inline
with `WrapInPanelMenu="false"`:

```razor
<RadzenPanelMenu>
    <Quilt4RadzenPanelMenuItem TextKey="menu.home" DefaultText="Home" Icon="home" Path="/" />
    <ContentMenu RoutePrefix="/pages/" WrapInPanelMenu="false" />
</RadzenPanelMenu>
```

Auto-refreshes on `NavigationManager.LocationChanged` so a menu that came up empty (server
unreachable at boot) recovers without a manual page reload. Re-fetches on language change too.

Parameters:

| Parameter | Description |
|-----------|-------------|
| `RoutePrefix` | Path-prefix for generated hrefs. Default `/`. Pass `/pages/` for the route above. |
| `WrapInPanelMenu` | `true` (default) emits its own `RadzenPanelMenu`; `false` emits just items for inline use. |
| `ShowEmptyPlaceholder` | `null` (default) shows `(no menu pages)` in development, hides in production. Override with `true`/`false`. |
| `EmptyPlaceholderText` | Placeholder text. Default `(no menu pages)`. |
| `Application` | Application override forwarded to the reader. |

Radzen quirk to know about: a `RadzenPanelMenuItem` with a `Path` is treated as a navigation
leaf and won't expand. So `<ContentMenu>` automatically omits `Path` on parents (items with
children). If you want a parent to also navigate, author an explicit child with the same slug.

### Breadcrumbs

`<ContentPageView>` pushes ancestors + the page itself into Tharga.Blazor's `BreadCrumbService`
via the `IPageBreadcrumbAdapter` abstraction (`TharBlazorBreadcrumbAdapter` is the default
implementation, registered automatically). To display them, add `<BreadCrumbs />` to your
layout — and register Tharga.Blazor in `Program.cs`:

```csharp
using Tharga.Blazor.Framework;

builder.Services.AddThargaBlazor(o => { o.Title = "My Site"; });
```

Hosts that don't register `AddThargaBlazor` get a silent no-op (the adapter looks up
`BreadCrumbService` optionally), so the reader still works without Tharga.Blazor's breadcrumb
system.

### Pages from code

Inject `IContentPageReader` to fetch programmatically:

```csharp
@inject IContentPageReader PageReader

var page = await PageReader.GetBySlugAsync("about", languageKey, application: null);
var menuItems = await PageReader.GetTreeAsync(languageKey);
```

The default-language sentinel is `Guid.Empty`. The reader returns `null` for a missing page and
an empty list for an empty/unreachable tree — neither throws.

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

In local mode the host picks the auth flavour via the `AuthMode` option: `ClientSecret` (TenantId + ClientId + ClientSecret), `ManagedIdentity` (Azure-hosted, no secret in config), or `DefaultAzureCredential` (one config that works locally via `az login` and in Azure via MI). See the [`Quilt4Net.Toolkit` README — Managed Identity / DefaultAzureCredential sections](../Quilt4Net.Toolkit/README.md#managed-identity) for the full table, `appsettings.json` shape, and the Azure IAM step (Log Analytics Reader). **All AI-querying components inherit whichever auth mode is configured** — `LogView`, `VersionMatrixDisplay`, `MetricsView`, `MetricChart`, `MetricLatestChart`, `LogCountView`, `LogCountByServiceView`, and `LogSummaryView` all go through one shared `LogsQueryClient` built once from the configured credential. No per-component setting.

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
| `Configs` | `null` | Explicit list of `ApplicationInsightsConfigurationResponse` to choose from (e.g. a team's workspaces). When more than one is supplied, `LogView` shows a dropdown and queries the selected one — so a host that already holds the workspaces can render `LogView` directly instead of wrapping it in a custom picker. Precedence: `Context` > selected `Configs` entry > built-in remote selector. |
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
| `Configs` | `null` | Explicit list of `ApplicationInsightsConfigurationResponse` to merge (e.g. a team's workspaces), with a multi-select bar (none selected = all). Lets a host that already holds the workspaces reuse this component instead of duplicating the grid. Precedence: `Context` > `Configs` > built-in remote selector. |
| `Lookback` | toolkit default | How far back to scan for version entries. |
| `EnvironmentOrder` | `ApplicationInsightsOptions.EnvironmentOrder` | Column (environment) ordering. |
| `AliasFolder` | `null` | Optional `raw → logical` folding delegate. When `null`, falls back to `ApplicationInsightsOptions.ApplicationAlias`. |
| `ConfigurationPath` | `null` | When set, an "Edit configuration" link is shown on authentication-failure alerts. |

When the team has more than one configured workspace (remote mode, or an explicit `Configs` list) it renders a built-in **multi-select radio bar**: toggle one or more workspaces and their version data is merged into a single matrix; selecting none shows all. (`LogView` uses a single-select dropdown for the same purpose.)

Two column toggles — **Show Development** and **Show Unknown** — are off by default. Each hides its environment column (`Development` / `(unknown)`) and drops any application row left without values in the remaining columns, so dev-only / unknown-only apps stay out of the default view. Toggling is instant (no re-query).

## Metrics view

`MetricsView` charts host and Kubernetes-cluster resource metrics read back from Application
Insights (the `AppMetrics` table). Quilt4Net **reads** these — it does not produce them; the
series come from an OpenTelemetry Collector (`hostmetrics` for machines, `kubeletstats` for the
cluster) exporting OTel `system.*` / `k8s.*` semantic-convention metrics into your workspace.

```razor
<MetricsView />
```

It uses the same Application Insights client and configuration selector as `LogView` (local or
remote mode — see [Log viewer](#log-viewer)), and a circuit-scoped cache so navigating away and
back is instant. A range selector (1h / 24h / 7d) and a Refresh button sit at the top.

**Hosts** (per machine, from `system.*`, grouped by `host.name`):

| Chart | Source |
|-------|--------|
| CPU % | `system.cpu.utilization` (idle → busy %) |
| Memory % | `system.memory.utilization` |
| Disk used (GB) | `system.filesystem.usage` (per host + volume, with capacity bars) |

**Cluster nodes** — shown only when the workspace has Kubernetes telemetry (otherwise the
section collapses), one line per node (`k8s.node.name`):

| Chart | Source | Unit |
|-------|--------|------|
| Node CPU | `k8s.node.cpu.usage` | cores (absolute — there is no node CPU-capacity metric to form a %) |
| Node memory % | `k8s.node.memory.usage / (usage + available)` | % |
| Node filesystem % | `k8s.node.filesystem.usage / capacity` | % |

**Node → pod drill-down** — pick a node from the dropdown to load the pods scheduled on it
(`k8s.pod.cpu.usage` in cores and `k8s.pod.memory.usage` in MB, labelled
`{namespace}/{pod}`). Pod series are fetched on demand and are not cached.

> Network throughput and swap/paging are intentionally not charted. Per-host load average
> (`system.cpu.load_average.*`) is also omitted: the collector currently emits it without a host
> resource attribute, so it can't be attributed to a machine — a producer-side fix is needed
> before it can be shown here.

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
