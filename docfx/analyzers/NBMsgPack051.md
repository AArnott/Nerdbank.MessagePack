# NBMsgPack051: Prefer modern .NET APIs

Some APIs exist to support targeting .NET Standard or .NET Framework, while other APIs are available when targeting .NET that are far superior.
This diagnostic is reported when code uses the lesser API while the preferred API is available.

The diagnostic message will direct you to the preferred API.

In multi-targeting projects where switching to the preferred API is inadvisable because it would break the build for older target frameworks or require the use of `#if` sections, you may suppress this warning.

## Example violation

The following type is declared using the non-generic @ShapeShift.KnownSubTypeAttribute.
This is fine for projects that target .NET Standard or .NET Framework, but if the project targets .NET a warning will be emitted.

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack051.cs#Defective)]

## Resolution

Per the message in the warning, switch to the generic attribute:

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack051.cs#SwitchFix)]

Or in a multitargeting project, use the preferred API only where it's available:

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack051.cs#MultiTargetingFix)]
