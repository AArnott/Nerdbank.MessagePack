# NBMsgPack033: Async converters should return writers

Custom converters (classes that derive from @"ShapeShift.MessagePackConverter`1") that override the @ShapeShift.MessagePackConverter`1.WriteAsync* method should [return](xref:ShapeShift.MessagePackAsyncWriter.ReturnWriter*) the @ShapeShift.MessagePackWriter struct that it [creates](xref:ShapeShift.MessagePackAsyncWriter.CreateWriter*).

Consider that the location of the diagnostic may not always indicate the location of the underlying issue.
When resolving this violation, consider the various branching, loops, etc. as the problem may only be present when the code takes certain code paths.

## Example violation

In the following @ShapeShift.MessagePackConverter`1.WriteAsync* method, a synchronous writer is created with @ShapeShift.MessagePackAsyncWriter.CreateWriter* but not returned at two points where it is necessary.

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack033.cs#Defective)]

## Resolution

Add calls to @ShapeShift.MessagePackAsyncWriter.ReturnWriter\* before any exit from the method or any `await` expression.

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack033.cs#Fix)]
