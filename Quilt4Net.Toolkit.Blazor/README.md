# Quilt4Net Toolkit Blazor
[![GitHub repo](https://img.shields.io/github/repo-size/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Quilt4/Quilt4Net.Toolkit)

Quilt4Net Toolkit Blazor uses [Radzen](https://blazor.radzen.com/).

## Get started
After having installed the nuget package.
Register *AddQuilt4Net* as a service and use it in the app.
```
var builder = WebApplication.CreateBuilder(args);
...
builder.Services.AddQuilt4Net(_ => builder.HostEnvironment.Environment, o =>
{
    o.ApiKey = "[ApiKey]";
});

await builder.Build().RunAsync();
```
You have to get an API key from [Quilt4Net](https://quilt4net.com).

## Feature Toggle

## Remote Configuration

## Content

To output text and html use the controllers. *Key* is used to identify the text.
It is possible to add a default entry, that will be inserted if no content exist.

Texts can be edited on [Quilt4Net](https://quilt4net.com) or directly on the site by setting *Enabled* property in *IEditContentService* to *true*.

Content is loaded by priority from environment. The environment names and order can be set up at [Quilt4Net](https://quilt4net.com). (IE. Development, Test, Production)
When the application is run locally, the default texts will be inserted with the default values.
When running the application in *Test* environment, the values from the underlaying environment, that in this case is *Development*.

If the text differs from underlaying environment, the updated can be promoted from *Development* to *Text* and then from *Test* to *Production*.

If a change is made to a higher version, like in *Test* or *Production* it will be automatically propagated to downstream environments.

### Use as controller

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