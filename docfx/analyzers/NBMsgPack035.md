# NBMsgPack035: Async converters should return readers

Custom converters (classes that derive from @"ShapeShift.MessagePackConverter`1") that override the @ShapeShift.MessagePackConverter`1.ReadAsync* method should [return](xref:ShapeShift.Converters.AsyncReader.ReturnReader*) the @ShapeShift.MessagePackReader or @ShapeShift.Converters.StreamingReader struct that it creates with @ShapeShift.Converters.AsyncReader.CreateBufferedReader* or @ShapeShift.Converters.AsyncReader.CreateStreamingReader*, respectively.

Consider that the location of the diagnostic may not always indicate the location of the underlying issue.
When resolving this violation, consider the various branching, loops, etc. as the problem may only be present when the code takes certain code paths.

## Example violation

In the following example, the @ShapeShift.Converters.Converter`1.ReadAsync\* method creates a @ShapeShift.Converters.StreamingReader then switches to a @ShapeShift.MessagePackReader.
Neither reader is returned, which is a bug.

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack035.cs#Defective)]

## Resolution

The streaming reader must be returned prior to switching to the buffered reader.
Both readers must be returned prior to the method exiting or awaited expressions.

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack035.cs#Fix)]

There is one exception to the rule of returning readers before they an `await` expression:
when that `await` expression is specifically for fetching more bytes to that same reader.
