# NBMsgPack070: UseComparerAttribute type must not be an open generic

<xref:Nerdbank.MessagePack.UseComparerAttribute> should specify a concrete type, not an open generic type.

## Example violation

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack070.cs#Defective)]

## Resolution

Specify a concrete type instead of an open generic type:

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack070.cs#Fix)]
