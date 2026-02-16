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

### Tabs

| Tab | Description |
|-----|-------------|
| **Search** | Free-text search across log entries. |
| **Summary** | Grouped view by error fingerprint with count and last occurrence. |
| **Measure** | Performance measurements with line charts (elapsed time by action). |
| **Count** | Log count statistics with column charts. |
| **Trigger** | Development utility for manually triggering log entries. |

## Configuration

All options can be set via code or `appsettings.json`. Code takes priority.

### ContentOptions

| Property | Default | Description |
|----------|---------|-------------|
| `Application` | Assembly name | Application name. |
| `Quilt4NetAddress` | `"https://quilt4net.com/"` | Quilt4Net server address. |
| `ApiKey` | `null` | API key from [Quilt4Net Web](https://quilt4net.com). |

Configuration path: `Quilt4Net:Content`
