# Built-in converters

The following types have explicit support built into the library because they are primitives or complex.

Most other types build on this list and will typically default to "Just Work" with this library without additional attributes.

Although these types have built-in support, a [type shape](type-shapes.md) is always required for the top-level type being serialized.

Enums, arrays and various dictionary types that utilize these types are implicitly supported, provided the type shape provider can produce shapes for them.

## Numbers

- @System.Byte
- @System.SByte
- @System.UInt16
- @System.Int16
- @System.UInt32
- @System.Int32
- @System.UInt64
- @System.Int64
- @System.Half
- @System.Single
- @System.Double
- @System.Decimal
- @System.Int128
- @System.UInt128
- @System.Numerics.BigInteger

## Text

- @System.String
- @System.Text.Rune
- @System.Char

## Time

- @System.DateTime
- @System.DateTimeOffset
- @System.DateOnly
- @System.TimeOnly
- @System.TimeSpan

## Complex types

- @System.Dynamic.ExpandoObject

## Other

- @System.Boolean
- @System.Nullable`1
- @System.Drawing.Color
- @System.Version
- @System.Uri
- @Nerdbank.MessagePack.RawMessagePack

## Optional converters

A number of optional converters are included but not active by default in order to keep the size of your application small and startup fast.
You can activate these converters in code when you need them using the extension methods on <xref:Nerdbank.MessagePack.OptionalConverters>.

Data type | API to enable
--|--
@System.Guid | <xref:Nerdbank.MessagePack.OptionalConverters.WithGuidConverter*>
@System.Text.Json.Nodes.JsonNode | <xref:Nerdbank.MessagePack.OptionalConverters.WithSystemTextJsonConverters*>
@System.Text.Json.JsonElement | <xref:Nerdbank.MessagePack.OptionalConverters.WithSystemTextJsonConverters*>
@System.Text.Json.JsonDocument | <xref:Nerdbank.MessagePack.OptionalConverters.WithSystemTextJsonConverters*>

For example, use this code to create a msgpack serializer that can convert System.Text.Json DOM types:

[!code-csharp[](../../samples/cs/BuiltInConverters.cs#STJOptionalConverters)]

Then use the serializer stored in that field to serialize and deserialize such objects.
