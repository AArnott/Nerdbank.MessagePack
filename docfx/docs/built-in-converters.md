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

- @System.DateTime - values with @System.DateTime.Kind?displayProperty=nameWithType left at @System.DateTimeKind.Unspecified will throw an exception by default.
  Use <xref:Nerdbank.MessagePack.OptionalConverters.WithAssumedDateTimeKind*> to allow serialization of such values.
- @System.DateTimeOffset
- @System.DateOnly
- @System.TimeOnly
- @System.TimeSpan

## Other

- @System.Boolean
- @System.Globalization.CultureInfo
- @System.Guid
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
@System.Dynamic.ExpandoObject | <xref:Nerdbank.MessagePack.OptionalConverters.WithExpandoObjectConverter*>
@System.Object | <xref:Nerdbank.MessagePack.OptionalConverters.WithObjectConverter*> or <xref:Nerdbank.MessagePack.OptionalConverters.WithDynamicObjectConverter*>
@System.Text.Json.Nodes.JsonNode | <xref:Nerdbank.MessagePack.OptionalConverters.WithSystemTextJsonConverters*>
@System.Text.Json.JsonElement | <xref:Nerdbank.MessagePack.OptionalConverters.WithSystemTextJsonConverters*>
@System.Text.Json.JsonDocument | <xref:Nerdbank.MessagePack.OptionalConverters.WithSystemTextJsonConverters*>

For example, use this code to create a msgpack serializer that can convert System.Text.Json DOM types:

[!code-csharp[](../../samples/cs/BuiltInConverters.cs#STJOptionalConverters)]

Then use the serializer stored in that field to serialize and deserialize such objects.

### String-based Guid serialization

By default, @System.Guid values are serialized in a compact binary format for maximum efficiency.
If you prefer to serialize GUIDs as strings (for human readability or compatibility), you can use <xref:Nerdbank.MessagePack.OptionalConverters.WithGuidConverter*> to specify the string format.
