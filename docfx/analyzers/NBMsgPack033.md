# NBMsgPack033: Async converters should return writers

Custom converters (classes that derive from @"Nerdbank.MessagePack.MessagePackConverter`1") that override the @Nerdbank.MessagePack.MessagePackConverter`1.WriteAsync* method should [return](xref:Nerdbank.MessagePack.MessagePackAsyncWriter.ReturnWriter*) the @Nerdbank.MessagePack.MessagePackWriter struct that it [creates](xref:Nerdbank.MessagePack.MessagePackAsyncWriter.CreateWriter*).

Consider that the location of the diagnostic may not always indicate the location of the underlying issue.
When resolving this violation, consider the various branching, loops, etc. as the problem may only be present when the code takes certain code paths.

## Example violation

In the following @Nerdbank.MessagePack.MessagePackConverter`1.WriteAsync* method, a synchronous writer is created with @Nerdbank.MessagePack.MessagePackAsyncWriter.CreateWriter* but not returned at two points where it is necessary.

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack033.cs#Defective)]

## Resolution

Add calls to @Nerdbank.MessagePack.MessagePackAsyncWriter.ReturnWriter* before any exit from the method or any `await` expression.

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack033.cs#Fix)]
