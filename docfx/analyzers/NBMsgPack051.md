# NBMsgPack051: Prefer modern .NET APIs

Some APIs exist to support targeting .NET Standard or .NET Framework, while other APIs are available when targeting .NET that are far superior.
This diagnostic is reported when code uses the lesser API while the preferred API is available.

The diagnostic message will direct you to the preferred API.

In multi-targeting projects where switching to the preferred API is inadvisable because it would break the build for older target frameworks or require the use of `#if` sections, you may suppress this warning.

## Example violation

The following code serializes a value using a shape provider parameter.
This method may fail at runtime if the shape provider fails to provide a shape for the type to be serialized.

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack051.cs#Defective)]

This is acceptable for projects that target .NET Standard or .NET Framework because compile-time enforcement is not available.
But if the project targets .NET a warning will be emitted to encourage use of the safer APIs.

## Resolution

Per the message in the warning, switch to the overload that takes a type that is constrained to guaranteeing availability of its shape:

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack051.cs#SwitchFix)]

Or in a multitargeting project, use the preferred API only where it's available:

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack051.cs#MultiTargetingFix)]

> [!TIP]
> In a multi-targeting project, simply reducing the severity of the diagnostic from Warning to something lesser may be a better option than creating `#if`/`#else`/`#endif` regions in many places.
