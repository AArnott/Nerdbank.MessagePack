# Trimming

.NET 8 introduced application trimming as an option, which is always on when targeting NativeAOT.
.NET 9 introduced [feature switches](https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.featureswitchdefinitionattribute?view=net-9.0) that allows an application to achieve smaller deployment sizes by removing code that is reachable but is not expected to be used by the application.

## Feature switches

Nerdbank.MessagePack is trim-friendly and NativeAOT safe.
It includes features that are on by default, but can be turned off at publish time of our application in order to potentially significantly shrink the size of your deployed application.
The following table lists these deactivatable features:

Feature | Description
--|--
`Feature.MessagePack.SystemTextJsonConverters` | Include converters for @System.Text.Json.JsonDocument, @System.Text.Json.JsonElement, @System.Text.Json.Nodes.JsonNode

## Sample use

You can remove a Nerdbank.MessagePack feature from your application by adding an item like this to your application's project file:

```xml
<ItemGroup>
    <RuntimeHostConfigurationOption Include="Feature.MessagePack.SystemTextJsonConverters" Value="false" />
</ItemGroup>
```

When testing the effectiveness if this switch, keep in mind the following:

1. This item is only effective when included in your application (exe) project.
1. The item is only effective on the output of `dotnet publish`.
1. Only .NET 9+ supports these feature switches.

Observe the resulting change in the size of your application binary (for NativeAOT) or the overall deployment directory (when merely trimming).
