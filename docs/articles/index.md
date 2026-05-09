# Articles

Feature guides for `Quilt4Net.Toolkit`. Skim the [Getting started](getting-started.md) page first if you're new — it gets you from `dotnet add package` to a running query against Application Insights. Otherwise jump to the topic that matches what you're solving:

- **[Getting started](getting-started.md)** — install, register, configure auth, your first query
- **[Telemetry identity & correlation](telemetry-identity.md)** — `AddQuilt4NetLogging`, the five attributes attached to every record, and the `X-Correlation-ID` propagation pattern that lets one id span client → server → response → client
- **[Log views](log-views.md)** — `LogView` and the Search / Summary / Detail / Test tabs, the multi-select filter bar with per-team browser persistence, the CorrelationId column, the Stack Trace tab with Resharper-friendly file:line copy, and the application-alias rendering
- **[Version matrix](version-matrix.md)** — drop-in Blazor component showing application × environment versions, with optional alias folding and per-environment ordering

Each guide is short — the full reference is in the [API](xref:Quilt4Net.Toolkit) tab.
