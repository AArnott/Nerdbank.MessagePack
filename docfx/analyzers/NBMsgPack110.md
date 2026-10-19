# NBMsgPack110: Add DefaultValueAttribute when using non-default initializers

This diagnostic is emitted for an optional serialized field or property that has a non-default initializer but no <xref:System.ComponentModel.DefaultValueAttribute>.
The member type must support C# constant values that can be represented as attribute arguments, such as a primitive, string, or enum.
Members of collection and other non-constant types are not reported.
Constant initializers equal to the serializer's omission default are not reported.
This is normally the member type's C# default value, such as `false`, numeric zero, `null`, or `default`, but a matching optional parameter in the deserializing constructor supplies its default instead.
Constant values are compared after conversion to the member type.
Constructor parameters are bound to declared member names before applying `[PropertyShape(Name = "...")]` or `[ParameterShape(Name = "...")]` overrides, and exact member-initializer matches take precedence over case-insensitive method-parameter matches.
Initializers that directly forward the parameter bound to that member in the deserializing primary constructor are also not reported, including parentheses and identity conversions.
Forwarding another member's parameter, or a parameter from a primary constructor not selected for deserialization, is not exempt.
This exemption does not cover expressions that transform the parameter value, non-identity conversions, or unrelated initializers in the same type.

The warning is particularly relevant when <xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeDefaultValues> is set to a value other than its default, <xref:Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Always>.
Under such a policy, the serializer may omit the C# default value for the member type, while deserialization leaves the member at the different value assigned by its initializer.
This can prevent values from round-tripping correctly.

Members required by the type-shape policy are not reported, including members marked with the C# `required` modifier or `[PropertyShape(IsRequired = true)]`.
An explicit `[PropertyShape(IsRequired = false)]` overrides the `required` modifier and makes the member eligible for this diagnostic again.

## Example violation

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack110.cs#Defective)]

## Resolution

Apply <xref:System.ComponentModel.DefaultValueAttribute> with the initialized value so the serializer uses the intended default:

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack110.cs#Fix)]

The code fix is available when the initializer is a constant expression and adds <xref:System.ComponentModel.DefaultValueAttribute> using a globally qualified attribute name and that expression, including an explicit cast when needed so the attribute stores the member's exact type.

### Required members as an alternative

When there is no stable constant default, mark the member with `[PropertyShape(IsRequired = true)]` or the C# `required` modifier instead.

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack110.cs#RequiredFix)]

A serialization policy containing <xref:Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Required> writes required members even when their values equal their defaults.
With <xref:Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Never>, required members may still be omitted, but normal deserialization rejects their absence instead of silently retaining a different initializer value.
Enabling <xref:Nerdbank.MessagePack.DeserializeDefaultValuesPolicy.AllowMissingValuesForRequiredProperties> disables that protection.
Making a member required changes its schema and can cause older payloads that omit it to be rejected.
The existing code fix only adds <xref:System.ComponentModel.DefaultValueAttribute> for constant initializers; marking a member as required is a manual alternative.
