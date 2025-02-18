# NBMsgPack037: Async converters should override PreferAsyncSerialization

Custom converters that override the @ShapeShift.MessagePackConverter`1.ReadAsync* or @ShapeShift.MessagePackConverter`1.WriteAsync\* methods should also override @ShapeShift.MessagePackConverter`1.PreferAsyncSerialization and have it return `true`.

## Example violation

The following converter overrides the async methods but doesn't override @ShapeShift.MessagePackConverter`1.PreferAsyncSerialization in order to indicate that its async methods are preferred:

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack037.cs#Defective)]

## Resolution

Simply add an override that returns `true`:

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack037.cs#Fix)]
