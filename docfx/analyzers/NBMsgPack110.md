# NBMsgPack110: Add DefaultValueAttribute when using non-default initializers

This diagnostic is emitted for a serialized field or property that has an initializer but no <xref:System.ComponentModel.DefaultValueAttribute>.
The member type must support C# constant values that can be represented as attribute arguments, such as a primitive, string, or enum.
Members of collection and other non-constant types are not reported.

The warning is particularly relevant when <xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeDefaultValues> is set to a value other than its default, <xref:Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Always>.
Under such a policy, the serializer may omit the C# default value for the member type, while deserialization leaves the member at the different value assigned by its initializer.
This can prevent values from round-tripping correctly.

## Example violation

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack110.cs#Defective)]

## Resolution

Apply <xref:System.ComponentModel.DefaultValueAttribute> with the initialized value so the serializer uses the intended default:

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack110.cs#Fix)]

The code fix is available when the initializer is a constant expression and adds <xref:System.ComponentModel.DefaultValueAttribute> using a globally qualified attribute name and that expression, including an explicit cast when needed so the attribute stores the member's exact type.
