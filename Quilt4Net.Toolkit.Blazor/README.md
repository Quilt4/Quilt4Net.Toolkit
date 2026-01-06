# Quilt4Net Toolkit Blazor
[![GitHub repo](https://img.shields.io/github/repo-size/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Quilt4/Quilt4Net.Toolkit)

Quilt4Net Toolkit Blazor uses [Radzen](https://blazor.radzen.com/).

[Quilt4Net.Toolkit](https://github.com/Quilt4/Quilt4Net.Toolkit/tree/master/Quilt4Net.Toolkit) is part of this package and can be used with this package as well.

## Get started

Install nuget package `Quilt4Net.Toolkit.Blazor`

Register *AddQuilt4NetBlazorContent* as a service and use it in the app.
```
var builder = WebApplication.CreateBuilder(args);
...
builder.Services.AddQuilt4NetBlazorContent(o =>
{
    o.ApiKey = "YOUR_API_KEY_HERE"; //Just use code configuration for testing.
});

await builder.Build().RunAsync();
```

You have to get an API key from [Quilt4Net](https://quilt4net.com).
The ApiKey can be placed in appsettings.json, in User Secrets or in code for testing.

```
{
  "Quilt4Net": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

## Using the Content feature

To output text and html use the controllers. *Key* is used to identify the text.
It is possible to add a default entry, that will be inserted if no content exist.

Texts can be edited on [Quilt4Net](https://quilt4net.com) or directly on the site by setting *Enabled* property in *IEditContentService* to *true*.

Content is loaded by priority from environment. The environment names and order can be set up at [Quilt4Net](https://quilt4net.com). (IE. Development, Test, Production)
When the application is run locally, the default texts will be inserted with the default values.
When running the application in *Test* environment, the values from the underlaying environment, that in this case is *Development*.

If the text differs from underlaying environment, the updated can be promoted from *Development* to *Text* and then from *Test* to *Production*.

If a change is made to a higher version, like in *Test* or *Production* it will be automatically propagated to downstream environments.

Texts can be translated manually or by AI.

### Use as components

Content can be loaded using specific controllers. Here are som examples.

- Quilt4Content
- Quilt4Text
- Quilt4Span
- Quilt4Raw
- Quilt4Button

Text field
```
<Quilt4Text Key="MyText1" Default="Header" TextStyle="TextStyle.H1" />
```

Content
```
<Quilt4Content Key="MyContent1">
    Some content with <i>link</i> to <a href="http://www.google.com" target="_blank">google</a>.
</Quilt4Content>
```

### Use from Code Behind

Inject *IContentService* and make a call to get content like this.

```
    var response = await _contentService.GetContentAsync("[Key], "[DefaultValue]", ContentFormat.String);
```

## Languages
When starting the content has a *default* language, that can be named.

Languages can be added and always derives from the *default* lanauge. When translation is completed the language can be activated to a specific environment. After that it can be promoted between environments.

When the *default* language changes after a translation, there will be a notification suggesting to chage.
