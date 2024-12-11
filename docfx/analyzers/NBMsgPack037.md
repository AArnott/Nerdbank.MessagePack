# NBMsgPack037: Async converters should override PreferAsyncSerialization

Custom converters that override the @Nerdbank.MessagePack.MessagePackConverter`1.ReadAsync* or @Nerdbank.MessagePack.MessagePackConverter`1.WriteAsync* methods should also override @Nerdbank.MessagePack.MessagePackConverter`1.PreferAsyncSerialization and have it return `true`.

## Example violation

The following converter overrides the async methods but doesn't override @Nerdbank.MessagePack.MessagePackConverter`1.PreferAsyncSerialization in order to indicate that its async methods are preferred:

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack037.cs#Defective)]

## Resolution

Simply add an override that returns `true`:

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack037.cs#Fix)]
