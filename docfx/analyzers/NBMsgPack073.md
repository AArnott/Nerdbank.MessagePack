# NBMsgPack073: UseComparerAttribute type must not be abstract unless using static member

<xref:Nerdbank.MessagePack.UseComparerAttribute> should not specify an abstract type unless a static member is also specified to provide the actual comparer instance.

## Example violation

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack073.cs#Defective)]

## Resolution

Either use a concrete type or specify a static member on the abstract type:

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack073.cs#Fix)]
