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
| `Default` | — | Fallback text. |
| `TextStyle` | `Body1` | Radzen text style. |
| `Visible` | `true` | Show or hide. |
| `Style` | `null` | Inline CSS. |

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

## Where next

- **[Log views](log-views.md)** — content-aware host of the AI log surface.
- **[Version matrix](version-matrix.md)** — app × env grid sourced from the same workspace.
