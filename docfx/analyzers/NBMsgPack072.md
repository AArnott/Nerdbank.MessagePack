# NBMsgPack072: UseComparerAttribute must specify a compatible comparer

<xref:Nerdbank.MessagePack.UseComparerAttribute> must specify a type or member that implements `IComparer<T>` or `IEqualityComparer<T>` appropriate for the decorated collection.

## Example violation

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack072.cs#Defective)]

## Resolution

Specify a type that implements the appropriate comparer interface:

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack072.cs#Fix)]
