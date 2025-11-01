# Targeted deserialization

Targeted deserialization allows you to deserialize only a fragment of msgpack, reducing the allocation and CPU cost of deserialization an entire object graph to just get at one value.

The <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializePath*?displayProperty=nameWithType> methods offer this targeted deserialization capability.
Like [streaming deserialization](streaming-deserialization.md), the targeting is based on LINQ expressions.
You simply describe the 'path' from the root of the object graph to the element you wish to deserialize in strongly-typed C# terms, and the serializer will quickly advance through the msgpack encoded bytes to find the data and deserialize only that element.

Here is an example:

[!code-csharp[](../../samples/cs/TargetedDeserialization.cs#Simple)]

Use the `!` null forgiveness operator when using nullable warnings as necessary:

[!code-csharp[](../../samples/cs/TargetedDeserialization.cs#Nullable)]

When the path finder cannot fully follow a path due to a missing property or a null value, the method will either throw or return `default(TElement)`, depending on the <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializePathOptions`2.DefaultForUndiscoverablePath?displayProperty=nameWithType> property.

The LINQ expression may include properties, indexers and dictionaries.
More complex LINQ expressions that may involve method calls or object creations will be rejected with a runtime exception.

The full cost of a targeted deserialization is the cost of deserializing the targeted element, plus the cost to decode the preceding msgpack sufficient to skip over unwanted structures.
Serialization converters are leveraged to understand how to translate an individual step in the LINQ expression into an advancement through the msgpack data.

A custom converter must override <xref:Nerdbank.MessagePack.MessagePackConverter.SkipToPropertyValue*> and/or <xref:Nerdbank.MessagePack.MessagePackConverter.SkipToIndexValue*> to participate in targeted deserialization.
