# NBMsgPack036: Async converters should not reuse readers after returning them

Custom converters (classes that derive from @"Nerdbank.MessagePack.MessagePackConverter`1") that override the @Nerdbank.MessagePack.MessagePackConverter`1.ReadAsync* method should not reuse a @Nerdbank.MessagePack.MessagePackReader or @Nerdbank.MessagePack.MessagePackStreamingReader after returning it via @Nerdbank.MessagePack.MessagePackAsyncReader.ReturnReader*.

## Example violation

In the following example, the @Nerdbank.MessagePack.MessagePackStreamingReader is returned before the method is done using it.

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack036.cs#Defective)]

## Resolution

The fix is simply to move the @Nerdbank.MessagePack.MessagePackAsyncReader.ReturnReader* call to a point after the method is done using the reader.

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack036.cs#Fix)]
