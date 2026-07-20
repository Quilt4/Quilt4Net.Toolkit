# Content components

Blazor components in `Quilt4Net.Toolkit.Blazor` for rendering content managed at [Quilt4Net Web](https://quilt4net.com). Two shapes:

- **Standalone components** drop a content-aware control directly into markup.
- **Content-aware Radzen wrappers** wrap an existing Radzen component and resolve one or two of its text attributes through the content service so the rest of the API stays exactly the same.

Every component below:

- Subscribes to `ILanguageStateService.LanguageChangedEvent` so the resolved text re-renders live on language switch.
- Falls back to the supplied default on miss / empty value / lookup failure — never throws.
- Follows the same naming: `{Property}Key` for the content key, `Default{Property}` for the fallback.

## Standalone components

### `<Quilt4Text>`

Plain text inside a Radzen `<RadzenText>` with a `TextStyle`.

```razor
<Quilt4Text Key="welcome.title" Default="Welcome" TextStyle="TextStyle.H1" />
```

| Parameter | Default | Description |
|---|---|---|
| `Key` | — | Content key. |
| `Default` | — | Fallback text (treated as the English/default-language default). |
| `Defaults` | `null` | Optional authoritative default text per language, keyed by two-letter ISO code (`"en"`, `"sv"`, …), resolved **locally**. Resolution chain: active-UI-culture code default → English (`"en"` or `Default`) → key. For correct wording before any translation exists. |
| `Translations` | `null` | Optional exact per-language values keyed by **language name** (e.g. `["Swedish"]="Ärende"`). Sent to the server and applied **only on first creation**: stored authoritative, AI skipped (0.10.4, #141). Distinct from `Defaults` (which is ISO-code + local-only). |
| `TextStyle` | `Body1` | Radzen text style. |
| `Visible` | `true` | Show or hide. |
| `Style` | `null` | Inline CSS. |

```razor
<Quilt4Text Key="case.subject"
            Default="Case subject"
            Defaults="@(new Dictionary<string,string> { ["en"] = "Case subject", ["sv"] = "Ärendemening" })" />
```

Also on the service: `IQuilt4ContentService.GetAsync(key, IReadOnlyDictionary<string,string> defaultsByLanguage, application)` (local per-language default, ISO-code keyed) and `GetAsync(key, defaultValue, IReadOnlyDictionary<string,string> translations, application)` (server-authoritative per-language values, language-name keyed; 0.10.3). The components' `Translations` parameter uses the latter. `Quilt4Span`, `Quilt4Raw`, `Quilt4PageTitle`, `Quilt4Tooltip` and the menu-item components (`Quilt4RadzenPanelMenuItem`, `Quilt4RadzenTabsItem`, `Quilt4SplitButton` + items) accept the same `Translations` parameter.

### `<Quilt4Content>`

HTML content. The default is provided as child content (rendered as `MarkupString` when no remote value exists).

```razor
<Quilt4Content Key="footer.copy">
    &copy; 2026 ACME Co. <a href="/privacy">Privacy</a>
</Quilt4Content>
```

### `<Quilt4Span>` / `<Quilt4Raw>`

Plain-string variants of `Quilt4Text`. `Quilt4Span` wraps in `<span>`; `Quilt4Raw` writes the resolved string directly with no wrapper.

```razor
<Quilt4Span Key="label.status" Default="Status:" />
<Quilt4Raw Key="value.ok" Default="OK" />
```

### `<Quilt4Button>`

A Radzen button with managed Text **and** an optional managed hover tooltip (HTML `title`). Especially useful for icon-only buttons.

```razor
<Quilt4Button TextKey="btn.submit" DefaultText="Submit" Icon="send" Click="@OnSubmit" />

<Quilt4Button Icon="delete"
              TooltipKey="btn.delete.tooltip" DefaultTooltip="Delete this row"
              Click="@OnDelete" />
```

| Parameter | Description |
|---|---|
| `TextKey` / `DefaultText` | Label content key + fallback. |
| `TooltipKey` / `DefaultTooltip` | Optional hover-tooltip content key + fallback. Set just the default for a static (non-localised) tooltip. |
| `Icon` | Radzen icon name. |
| `Click` | `Func<Task>` click handler. |
| `Style` | Inline CSS. |
| `Disabled` | Disables the button (mirrors `RadzenButton.Disabled`). |
| `Busy` | Shows an in-button spinner and blocks clicks while set (mirrors `RadzenButton.IsBusy`) — set it around a slow async `Click`. |
| `BusyTextKey` / `DefaultBusyText` | Optional busy-label content key + fallback shown while `Busy` (e.g. "Saving…"). Leave both unset to keep the normal label next to the spinner. |

### `<Quilt4SplitButton>`

A `RadzenSplitButton` with managed text on the primary button **and** every drop-down item — a primary action plus a menu of secondary actions, all content-localized. Drops the manual `IQuilt4ContentService.GetAsync` label plumbing per item.

```razor
<Quilt4SplitButton TextKey="row.open" DefaultText="Open" Icon="folder_open"
                   Click="@OnOpen"
                   Items="_actions" ItemClick="@OnAction" />

@code {
    private readonly IReadOnlyList<Quilt4SplitButtonItem> _actions =
    [
        new() { TextKey = "row.rename", DefaultText = "Rename", Value = "rename", Icon = "edit" },
        new() { TextKey = "row.delete", DefaultText = "Delete", Value = "delete", Icon = "delete" },
    ];

    private Task OnAction(string value) => /* dispatch on value */;
}
```

| Parameter | Description |
|---|---|
| `TextKey` / `DefaultText` | Primary button label content key + fallback. |
| `Icon` | Radzen icon name for the primary button. |
| `Click` | `Func<Task>` handler for the primary button. |
| `Items` | Drop-down items (`Quilt4SplitButtonItem`): each has `TextKey` / `DefaultText`, `Value`, `Icon`, `Disabled`. |
| `ItemClick` | `Func<string, Task>` invoked with the chosen item's `Value`. |
| `Disabled` | Disables the whole split button. |
| `Busy` / `BusyTextKey` / `DefaultBusyText` | In-button spinner + optional localized busy label (as `Quilt4Button`). |
| `Style` | Inline CSS. |

### `<Quilt4PageTitle>`

Wraps Blazor's `<PageTitle>` with a content-aware title.

```razor
<Quilt4PageTitle Key="page.about.title" Default="About us" />
```

### `<Quilt4Tooltip>`

A `<span title="...">` wrapper that pulls its tooltip text from the content service. Use on any element that doesn't already have a `TooltipKey` parameter (custom controls, links, icons, plain `<div>` etc.).

```razor
<Quilt4Tooltip TooltipKey="status.idle.tooltip" DefaultTooltip="Service is idle">
    <i class="rzi rz-icon-check" />
</Quilt4Tooltip>
```

| Parameter | Description |
|---|---|
| `TooltipKey` / `DefaultTooltip` | Tooltip content key + fallback. |
| `ChildContent` | The element(s) the tooltip is attached to. |
| `Style` | Inline CSS on the wrapping span. |

## Content-aware Radzen wrappers

Same parameter shape you'd pass to the underlying Radzen control, **plus** a content-aware pair for one or two text attributes. Everything else is pass-through unchanged.

### `<Quilt4RadzenAlert>`

Wraps `<RadzenAlert>`. Resolves `Text` (required) and `Title` (optional).

```razor
<Quilt4RadzenAlert AlertStyle="AlertStyle.Info"
                   TextKey="alert.permission.body"
                   DefaultText="You don't have permission to do that."
                   TitleKey="alert.permission.title"
                   DefaultTitle="Permission denied" />
```

Title is omitted entirely (no empty title bar) when neither `TitleKey` nor `DefaultTitle` is set.

### `<Quilt4RadzenDataGridColumn TItem="...">`

Wraps `<RadzenDataGridColumn>`. Resolves `Title`. Drop in inline inside a `<RadzenDataGrid>` (or `Quilt4RadzenDataGrid`).

```razor
<RadzenDataGrid TItem="Customer" Data="@_customers">
    <Columns>
        <Quilt4RadzenDataGridColumn TItem="Customer"
                                    Property="@nameof(Customer.Name)"
                                    TitleKey="col.customer.name"
                                    DefaultTitle="Name" />
    </Columns>
</RadzenDataGrid>
```

Pass-through: `Property`, `Width`, `Sortable`, `Filterable`, `Visible`, `SortOrder`, `TextAlign`, `FormatString`, `Template`.

### `<Quilt4RadzenDataGrid TItem="...">`

Wraps `<RadzenDataGrid>`. Resolves `EmptyText`. Use it when the empty state is plain text; for richer empty content, pass an `EmptyTemplate` and Radzen prefers the template over the resolved text.

```razor
<Quilt4RadzenDataGrid TItem="Customer" Data="@_customers"
                      EmptyTextKey="grid.customers.empty"
                      DefaultEmptyText="No customers yet.">
    <Columns>
        <RadzenDataGridColumn TItem="Customer" Property="@nameof(Customer.Name)" Title="Name" />
    </Columns>
</Quilt4RadzenDataGrid>
```

> **Surface scope.** This wrapper forwards only the most-used `RadzenDataGrid` parameters (sorting / filtering / paging / `Data` / `Columns` / `EmptyTemplate`). For finer control, use `<RadzenDataGrid>` directly with `<Quilt4Text>` inside its `EmptyTemplate`.

### `<Quilt4RadzenPanelMenuItem>`

Wraps `<RadzenPanelMenuItem>`. Resolves `Text`.

```razor
<RadzenPanelMenu>
    <Quilt4RadzenPanelMenuItem TextKey="menu.home" DefaultText="Home" Icon="home" Path="/" />
    <Quilt4RadzenPanelMenuItem TextKey="menu.customers" DefaultText="Customers" Icon="people" Path="/customers" />
</RadzenPanelMenu>
```

### `<Quilt4RadzenTabsItem>` and `<Quilt4RadzenLabel>`

Wrap `<RadzenTabsItem>` and `<RadzenLabel>` respectively. Both resolve `Text`.

```razor
<RadzenTabs>
    <Tabs>
        <Quilt4RadzenTabsItem TextKey="tab.overview" DefaultText="Overview">...</Quilt4RadzenTabsItem>
        <Quilt4RadzenTabsItem TextKey="tab.activity" DefaultText="Activity">...</Quilt4RadzenTabsItem>
    </Tabs>
</RadzenTabs>

<Quilt4RadzenLabel TextKey="form.name.label" DefaultText="Name" Component="Name" />
<Quilt4RadzenTextBox Name="Name" @bind-Value="@_name" />
```

### Input placeholders: `<Quilt4RadzenTextBox>` / `<Quilt4RadzenTextArea>` / `<Quilt4RadzenDropDown TValue="...">` / `<Quilt4RadzenNumeric TValue="...">`

The four input wrappers each resolve a single `Placeholder` attribute. Everything else — value binding, validation, change events, type-specific controls — is straight pass-through.

```razor
<Quilt4RadzenTextBox @bind-Value="@_name"
                     PlaceholderKey="input.name" DefaultPlaceholder="Type your name..." />

<Quilt4RadzenTextArea @bind-Value="@_notes"
                      PlaceholderKey="input.notes" DefaultPlaceholder="Notes..."
                      Rows="4" />

<Quilt4RadzenDropDown TValue="string" Data="@_countries"
                      @bind-Value="@_country"
                      PlaceholderKey="input.country" DefaultPlaceholder="Choose country" />

<Quilt4RadzenNumeric TValue="int" @bind-Value="@_quantity"
                     PlaceholderKey="input.quantity" DefaultPlaceholder="Quantity"
                     Min="1" Max="100" />
```

Common content-aware parameters across all four:

| Parameter | Description |
|---|---|
| `PlaceholderKey` | Content key for the input's placeholder. |
| `DefaultPlaceholder` | Fallback placeholder. |

Plus per-control pass-through: `Value` / `ValueChanged` / `Name` / `Disabled` / `ReadOnly` / `Change` on all; `Rows` / `Cols` on TextArea; `Data` / `TextProperty` / `ValueProperty` / `Multiple` / `AllowFiltering` / `AllowClear` on DropDown; `Min` / `Max` / `Step` / `Format` / `ShowUpDown` on Numeric.

## Content-aware Radzen services

For the two Radzen services that take user-facing text as method arguments rather than as component attributes, `AddQuilt4NetBlazorContent` also registers content-aware wrappers.

### `IQuilt4DialogService`

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
        if (ok != true) return;
        await DoDelete();
    }
}
```

Both `ConfirmAsync` and `AlertAsync` resolve the keys via the content service, then call Radzen's `DialogService.Confirm` / `Alert`. The return contract (`bool?` / `void`) is unchanged.

### `IQuilt4NotificationService`

Notifications whose summary and (optional) detail come from content keys.

```razor
@inject IQuilt4NotificationService Q4Notifications

@code {
    private async Task OnSaved()
    {
        await Q4Notifications.NotifyAsync(
            NotificationSeverity.Success,
            summaryKey: "save.success.summary", defaultSummary: "Saved",
            detailKey: "save.success.detail", defaultDetail: "Your changes have been saved.");
    }
}
```

| Parameter | Description |
|---|---|
| `severity` | Radzen `NotificationSeverity` (Info / Success / Warning / Error). |
| `summaryKey` / `defaultSummary` | Required summary content key + fallback. |
| `detailKey` / `defaultDetail` | Optional detail pair. Both null → no detail line. |
| `duration` | Display duration in ms (default 3000). |

## Startup warm-up

On a cold cache every content component issues its own HTTP lookup, so a content-heavy page (a menu,
a dashboard) fans out into dozens of requests that trickle in as they complete. To avoid that,
`AddQuilt4NetBlazorContent` registers a startup warm-up that pre-fills the cache in a **single bulk
call per language**:

- The **default language** is warmed in the background at application start (non-blocking — startup
  is not delayed).
- The user's **selected language** is warmed per circuit as soon as it's known, and again whenever
  they switch language. The cache is process-wide, so the first user on a language pays the one bulk
  call and everyone after hits the warm cache.
- Warming is **best-effort and backward-compatible**: against a server that doesn't expose the bulk
  endpoint (404), or on any failure/timeout, it silently falls back to the existing per-key fetching.

Disable it to rely purely on lazy per-key loading:

```json
{
  "Quilt4Net": {
    "Content": {
      "WarmUpEnabled": false
    }
  }
}
```

### Inspecting what's loaded

The content admin panel (`<ContentAdmin>`) shows content admins a **Loaded content per language**
list — the number of cached content entries per language, with a Refresh button — so you can confirm
the warm-up populated the cache as expected.

## Seeing where each value came from

A rendered string gives no clue whether it is real content or the hard-coded fallback the component
was written with. Turning on **Show content source** (in the `LanguageSelector` menu, admin only, or
`IContentSourceService.Enabled` from code) outlines every value and adds a tooltip naming its origin.

```csharp
@inject IContentSourceService ContentSourceService

ContentSourceService.Enabled = true;
```

Red means the server has no value for that key, so what you are reading is the component's default —
usually the thing you are looking for. Green is a fresh server fetch, blue a cache hit, amber a stale
cache entry being refreshed in the background.

Supported on `<Quilt4Text>`, `<Quilt4Span>`, `<Quilt4Raw>` and `<Quilt4Content>`. Edit mode takes
precedence when both are on, and `<Quilt4Raw>` gains a wrapping `<span>` only while the overlay is
enabled — with it off, markup is exactly as before.

For the same information without a browser — aggregated across an environment rather than per render
— see the content log levels in the
[Quilt4Net.Toolkit README](https://github.com/Quilt4/Quilt4Net.Toolkit/blob/master/Quilt4Net.Toolkit/README.md#diagnostic-logging):
an unseeded key logs at `Information` once per key, and a missing API key logs a `Warning` once per
process.

> Not to be confused with **developer mode**, labelled "Debug mode" in the same menu, which swaps
> every value for a placeholder to reveal unmanaged text. The source overlay leaves the text alone
> and only annotates it.

## Where next

- **[Log views](log-views.md)** — content-aware host of the AI log surface.
- **[Version matrix](version-matrix.md)** — app × env grid sourced from the same workspace.
