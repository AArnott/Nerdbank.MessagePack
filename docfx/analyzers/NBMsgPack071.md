# NBMsgPack071: UseComparerAttribute member name must point to a valid property

When <xref:Nerdbank.MessagePack.UseComparerAttribute> specifies a member name, it must point to a public property on the specified type.

## Example violation

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack071.cs#Defective)]

## Resolution

Ensure the member name points to a public property:

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack071.cs#Fix)]
