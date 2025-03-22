# Built-in converters

The following types have explicit support built into the library because they are primitives or complex.

Most other types build on this list and will typically default to "Just Work" with this library without additional attributes.

Although these types have built-in support, a [type shape](type-shapes.md) is always required for the top-level type being serialized.

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

- @System.Text.Json.Nodes.JsonNode
- @System.Text.Json.JsonElement
- @System.Text.Json.JsonDocument

## Other

- @System.Boolean
- @System.Nullable`1
- @System.Drawing.Color
- @System.Version
- @System.Uri
- @System.Guid
- @Nerdbank.MessagePack.RawMessagePack

Enums, arrays and various dictionary types that utilize these types are implicitly supported.
