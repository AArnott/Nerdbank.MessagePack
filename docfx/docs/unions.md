# Polymorphic serialization

A value can be one of several types.
Pick an approach below, then read that section.
F# unions are documented in [F#](fsharp.md).

## Choosing a union approach

| You have | Use |
| --- | --- |
| A base type and derived types you control | [Derived type hierarchies](#derived-type-hierarchies) |
| A closed set of unrelated types | [C# unions](#c-unions) |
| An existing schema with no type tag | [Duck typing](#duck-typing) |
| Types you cannot annotate | [Runtime configuration](#runtime-configuration) |

## Derived type hierarchies

Use a derived-type hierarchy when the declared type is a base class or interface and the runtime value is a derived type.

### Declaring derived types

Without any union metadata, only members declared on the base type are written.
If the base type is abstract, that payload cannot be deserialized.

[!code-csharp[](../../samples/cs/Unions.cs#LossyFarm)]

[!code-json[](../../samples/cs/Output/Unions.LossyFarm.json)]

Apply <xref:PolyType.DerivedTypeShapeAttribute> to the declared base type so each derived type keeps its own members and a type tag:

[!code-csharp[](../../samples/cs/Unions.cs#RoundtrippingFarmAnimal)]

[!code-json[](../../samples/cs/Output/Unions.RoundtrippingFarmAnimal.json)]

The union wrapper is used only when the declared type is the union.
A property typed as `Horse` is written as a plain object:

[!code-csharp[](../../samples/cs/Unions.cs#HorsePen)]

[!code-json[](../../samples/cs/Output/Unions.HorsePen.json)]

Once `Horse` itself lists derived types, each horse gains a tag:

[!code-csharp[](../../samples/cs/Unions.cs#HorseBreeds)]

[!code-json[](../../samples/cs/Output/Unions.HorseBreeds.json)]

### Inferring cases from closed hierarchies

<xref:PolyType.TypeShapeAttribute.InferClosedTypePolymorphism?displayProperty=nameWithType> discovers the cases of a C# 15 `closed` hierarchy, including terminal types nested under a closed derived type.
Explicit <xref:PolyType.DerivedTypeShapeAttribute> registrations win when you need a stable name or integer tag.

[!code-csharp[](../../samples/cs/Unions.cs#InferredFarmAnimal)]

[PolyType's closed hierarchies](https://eiriktsarpalis.github.io/PolyType/docs/shape-providers.html#closed-class-hierarchies) describes the inference rules.

### Multi-level hierarchies

A derived type that is itself a union adds another tag:

[!code-csharp[](../../samples/cs/Unions.cs#MultiLevel)]

[!code-json[](../../samples/cs/Output/Unions.MultiLevel.json)]

Register every transitive type on the original base to keep a single tag:

[!code-csharp[](../../samples/cs/Unions.cs#FlattenedAnimal)]

[!code-json[](../../samples/cs/Output/Unions.FlattenedAnimal.json)]

### Unknown derived types

An unregistered runtime type is written as the nearest registered base.
Deserialization creates that base type, not the unregistered one.
Rules that lived only on the missing derived type are not enforced.
Register the type on the union before its own members can round-trip.

### Case identifiers

The default identifier is the case type's name.
The base type's identifier is `nil`.
Set `Name` or `Tag` when a rename or a smaller payload matters.
String identifiers are case sensitive.
Mixing strings and integers on one union is allowed.

[!code-csharp[](../../samples/cs/Unions.cs#StringAliasTypes)]

[!code-csharp[](../../samples/cs/Unions.cs#IntAliasTypes)]

[!code-csharp[](../../samples/cs/Unions.cs#MixedAliasTypes)]

An unknown identifier throws <xref:Nerdbank.MessagePack.MessagePackSerializationException>.
A concrete base with a readable and writable <xref:Nerdbank.MessagePack.UnusedDataPacket> can instead keep the raw discriminator and unrecognized members and write them back.
Known base members are reserialized with the local settings, so this is not a byte-for-byte copy.
See [retaining unrecognized data](customizing-serialization.md#retaining-unrecognized-data).

### Wire format

The default wrapper is a two-element array, `[identifier, value]`.
<xref:Nerdbank.MessagePack.MessagePackSerializer.UseDiscriminatorObjects> writes a one-entry map instead.
Use the same setting on every reader and writer.

[!code-csharp[](../../samples/cs/Unions.cs#UseDiscriminatorObjects)]

[!code-json[](../../samples/cs/Output/Unions.WireFormat.json)]

The first line is the default array wrapper.
The second line is the object wrapper for the same value.

### Generic derived types

A case may be a closed generic type.
An open generic case is supported when its type arguments are determined by the base type.

[!code-csharp[](../../samples/cs/Unions.cs#ClosedGenericSubTypes)]

[!code-csharp[](../../samples/cs/Unions.cs#OpenGenericSubTypes)]

## C# unions

C# 15 `union` types need no union attribute beyond the shape you already generate.
Cases do not have to share a base type.

[!code-csharp[](../../samples/cs/Unions.cs#CSharpUnion)]

[!code-csharp[](../../samples/cs/Unions.cs#CSharpUnionUsage)]

> [!NOTE]
> These samples require C# 15 or later.

* `bool`, `string`, `char`, the 8- through 64-bit integers, `float`, `double`, and a single collection or binary case are written without a tag.
  Several integer cases use each type's exact-width encoding; one integer case uses the shortest encoding.
  `float` and `double` stay distinct by their native encodings.
  `char` and `ushort` share an encoding, so at least one needs an alias.
  On read, an exact-width match wins; otherwise the smallest case that can hold the value wins, preferring signed when the widths are equal.
* Objects, nested unions, custom converters, and any case with a [custom identifier](#custom-case-identifiers) keep a tag.
  The default tag is the case name, or an inferred integer when <xref:Nerdbank.MessagePack.MessagePackSerializer.PerfOverSchemaStability> is set.
  Renaming a tagged case changes its string tag, and reordering cases changes inferred integers.
* A missing reference union is bare `nil`.
  A present case with a null value keeps its tag, for a scalar or an object, so it is not read as a missing union.
  A struct union cannot be missing. Bare `nil` selects the untagged case that admits null.
* A union value that matches no case cannot be serialized.
* Unions may be recursive (`union Tree(int, Tree[])`).
* At most one collection case may be untagged.
  An untagged array selects map tags for the other cases, and an untagged dictionary selects array tags.
  That choice overrides <xref:Nerdbank.MessagePack.MessagePackSerializer.UseDiscriminatorObjects> for that union, including aliased cases.
  Adding or removing an untagged collection case changes those tags, so tag the collection when the protocol must stay stable.
* Untagged object payloads are not chosen by duck typing.
  Types that follow the union member pattern without the `union` keyword are supported.
  This encoding does not change derived-type or F# union bytes.

### Custom case identifiers

Set <xref:Nerdbank.MessagePack.MessagePackSerializer.UnionTypeAliasProvider> to give a C# union case a stable string or integer tag.
An alias forces a tag, including for a scalar or collection, and that case then accepts only the alias.
Returning no alias keeps that case's normal encoding.
An alias takes precedence over <xref:Nerdbank.MessagePack.MessagePackSerializer.PerfOverSchemaStability> and must be unique within the union, including tags inferred for the other cases.
The same payload type may use a different alias in another union.
The provider must be thread-safe and return the same answer for the life of the serializer.
It does not apply to derived-type or F# unions.

[!code-csharp[](../../samples/cs/UnionCaseAliases.cs#PrimitiveUnionCaseAliases)]

[!code-csharp[](../../samples/cs/UnionCaseAliases.cs#UnionCaseAliasTypes)]

[!code-csharp[](../../samples/cs/UnionCaseAliases.cs#UnionCaseAliasMapping)]

[!code-csharp[](../../samples/cs/UnionCaseAliases.cs#UnionCaseAliasRegistry)]

## Duck typing

Duck typing reads a name-keyed map as one of several object types by the required properties that are present.
There is no type tag.
It is slower than a tag because the map may be buffered to inspect its keys.
The API is experimental.

Enable it at runtime with <xref:Nerdbank.MessagePack.DerivedTypeDuckTyping>.
Register at least two concrete object types.
A concrete base type is one of the cases.
An abstract or interface base is not a case, and it is not a fallback.

[!code-csharp[](../../samples/cs/Unions.cs#DuckTyping)]

[!code-json[](../../samples/cs/Output/Unions.DuckTyping.json)]

A case matches when every required serialized name is present.
Extra names do not reject it.
If several cases match, the one whose required names are a strict superset of the others is chosen.
If no match is more specific, deserialization throws <xref:Nerdbank.MessagePack.MessagePackSerializationException>.
A case with no required properties is a catch-all: at most one is allowed, and it is used only when nothing else matches.

Required names are the names the object converter writes for required constructor parameters, `required` members, and <xref:PolyType.PropertyShapeAttribute.IsRequired>.
A required constructor parameter must match a property, and a required property must have a getter. Otherwise the object converter does not write that member, so that case is rejected when the union is built.
A required getter-only collection is also rejected when the type has a parameterized constructor, unless the collection is one of those parameters. That converter writes the collection and does not read it back.
A case with its own converter is rejected when the converter is built. That converter may not write the required names, so the payload can be read as a different case.
A required name that the object converter does not write is rejected for the same reason. A custom converter on a getter-only collection can omit that property.
<xref:Nerdbank.MessagePack.MessagePackSerializer.PropertyNamingPolicy> applies, except when <xref:PolyType.PropertyShapeAttribute.Name> sets the name.
Comparison is ordinal.
Two cases that require the same names are rejected when the union is built, including when a naming policy makes them the same.
Writing a runtime type that was not registered throws.
`nil` deserializes as null.
Any other non-map throws.

Tuples, <xref:Nerdbank.MessagePack.KeyAttribute>, and <xref:Nerdbank.MessagePack.MessagePackSerializer.PerfOverSchemaStability> are not supported, because those layouts are not name-keyed maps.

## Runtime configuration

Register derived types in code when you cannot apply attributes: third-party types, plugins, or a base type in another assembly.
You still assign each case a unique identifier.

# [.NET](#tab/net)

[!code-csharp[](../../samples/cs/Unions.cs#RuntimeSubTypesNET)]

# [.NET Standard](#tab/netfx)

[!code-csharp[](../../samples/cs/Unions.cs#RuntimeSubTypesNETFX)]

---

Runtime registration does not replace [type shapes](type-shapes.md).
Use it when the shapes have no derived-type metadata, or when that metadata is incomplete.

### Removing a union at runtime

<xref:Nerdbank.MessagePack.DerivedTypeUnion.CreateDisabled(System.Type)> ignores union attributes on a base type.

[!code-csharp[](../../samples/cs/Unions.cs#RuntimeSubTypesDisabled)]

## Custom converters

A converter on the base type of a union has extra rules.
See [custom converters](custom-converters.md#union-types).
