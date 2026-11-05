# Polymorphic serialization

Serialization of polymorphic types requires special consideration.

For instance, suppose you want to serialize a `Farm`:

[!code-csharp[](../../samples/cs/Unions.cs#LossyFarm)]

Notice that your animals on the farm are kept in a collection typed with the base type `Animal`.
At runtime, we expect most or all animals to be of a derived type rather than the `Animal` base type.

By default, serializing the `Farm` will only serialize animals with the properties that are directly on the `Animal` class.

```json
{
    "Animals": [
        { "Name": "Bessie" },
        { "Name": "Lighting" },
        { "Name": "Rover" },
    ]
}
```

Note the lack of any type information or properties defined on the derived types.
Deserializing this `Farm` will produce a bunch of `Animal` objects.
If `Animal` were an `abstract` class, this would not be deserializable at all.

You can preserve polymorphic type metadata across serialization using the <xref:PolyType.DerivedTypeShapeAttribute>, which you apply to the type that is used as the declared base type.
Since `Animal` is used as the collection element type, we apply the attributes on the declaration of that type:

[!code-csharp[](../../samples/cs/Unions.cs#RoundtrippingFarmAnimal)]

This changes the schema of the serialized data to include a tag that indicates the type of the object.
It also engages a converter for the specific derived type so that its unique properties are serialized.

Now the Farm serializes like this:

```json
{
    "Animals": [
        ["Cow",   { "Name": "Bessie",   "Weight": 1400 }],
        ["Horse", { "Name": "Lighting", "Speed": 45 }],
        ["Dog",   { "Name": "Rover",    "Color": "Brown" }],
    ]
}
```

Notice how each `Animal` is serialized with its derived type properties.
Each `Animal` object is also nested as the second element in an array whose first element identifies the derived type of the original object.

Deserializing this will recreate each object with its original derived type and full set of properties.
It works even if `Animal` is an `abstract` class.

## Inferring union cases automatically

Consider using <xref:PolyType.TypeShapeAttribute.InferClosedTypePolymorphism?displayProperty=nameWithType> to automatically infer union cases for closed type hierarchies.
This requires C# 15's `closed` modifier.
The compiler records the permitted derived types, allowing PolyType to discover the union cases without listing each one with <xref:PolyType.DerivedTypeShapeAttribute>.

Here is the same farm modeled with a closed animal hierarchy:

[!code-csharp[](../../samples/cs/Unions.cs#InferredFarmAnimal)]

Serializing this `Farm` preserves the `Cow`, `Horse`, and `Dog` types and their properties, just like the explicitly registered farm above.
The `closed` modifier prevents additional animal types from being defined outside the hierarchy permitted by the compiler.
If a derived type is itself `closed`, PolyType recursively discovers its terminal, non-closed derived types, producing a flat union rather than nested unions.

[Learn more about inferring union cases automatically](https://eiriktsarpalis.github.io/PolyType/docs/shape-providers.html#closed-class-hierarchies) from PolyType documentation.

Explicit <xref:PolyType.DerivedTypeShapeAttribute> registrations take precedence over inference.
Use explicit registrations when you need to control case names or assign stable numeric tags, since inferred numeric tags can change when the hierarchy changes.

## Unions

A type that stands in for itself and/or a collection of derived types is called a union.
The embellished schema where the object's serialized data is nested inside an array as shown above is used whenever the union type is the declared type (i.e. the type statically discoverable rather than requiring live objects).

When a class has a property typed as `Horse` is serialized, the `Horse` is serialized without the union schema because `Horse` is not itself a union type, even though it may appear as a case of another union type (e.g. `Animal`).
Only when `Animal` is the declared property type will a `Horse` object set to that property be serialized with the surrounding union schema.

For example, consider this collection of horses:

[!code-csharp[](../../samples/cs/Unions.cs#HorsePen)]

This would serialize like this:

```json
{
    "Horses": [
        { "Name": "Lighting", "Speed": 45 },
        { "Name": "Flash", "Speed": 48 },
    ]
}
```

Note the lack of the union schema that would add `["Horse", ... ]` around every horse.
This is because the `Horse` type is statically known as the generic type argument of the collection, and there's no need to add serialized data to indicate the runtime type.

Now suppose you have different breeds of horses that each had their own derived type:

[!code-csharp[](../../samples/cs/Unions.cs#HorseBreeds)]

At this point your `HorsePen` *would* serialize with the union schema around each horse:

```json
{
    "Horses": [
        ["QuarterHorse", { "Name": "Lighting", "Speed": 45 }],
        ["Thoroughbred", { "Name": "Flash", "Speed": 48 }],
    ]
}
```

## Multi-level union types

Now let's consider our original `Farm` class, which has a collection of `Animal` objects.
The `Animal` class as we defined it earlier only knows about `Horse` as a derived type.
The `Animal` class itself has no designation for `QuarterHorse` or `Thoroughbred`.

If the `Horse` class lacked any <xref:PolyType.DerivedTypeShapeAttribute> of its own, serializing your `Farm` would drop any details about horse breeds and deserializing would produce `Horse` objects where the original object graph may have contained `QuarterHorse` or `Thoroughbred`.
But if the `Horse` class has attributes for each of its derived types, we end up with a multi-nested union schema for our farm:

```json
{
    "Animals": [
        ["Cow",   { "Name": "Bessie",   "Weight": 1400 }],
        ["Horse", ["QuarterHorse", { "Name": "Lighting", "Speed": 45 }]],
        ["Horse", ["Thoroughbred", { "Name": "Flash", "Speed": 48 }]],
        ["Dog",   { "Name": "Rover",    "Color": "Brown" }],
    ]
}
```

You can avoid the multi-level nesting by defining all transitive derived types on the original union type `Animal`:

[!code-csharp[](../../samples/cs/Unions.cs#FlattenedAnimal)]

This would now serialize more simply as:

```json
{
    "Animals": [
        ["Cow",          { "Name": "Bessie",   "Weight": 1400 }],
        ["QuarterHorse", { "Name": "Lighting", "Speed": 45 }],
        ["Thoroughbred", { "Name": "Flash",    "Speed": 48 }],
        ["Dog",          { "Name": "Rover",    "Color": "Brown" }],
    ]
}
```

This simpler serialized form comes at the cost of maintaining a list of attributes for all transitively derived types on the original union type.

## Unknown derived types

When serializing a derived type that is not listed on its base union type, the nearest listed base type is selected as its union case instead.
For example, consider our flattened `Animal` class definition given earlier, where `Horse` and its two derived types are documented via attributes on the `Animal` class.
Now suppose we declare a new `Horse`-derived type called `Arabian`, but we omit adding an attribute for that derived type on `Animal`.
When an `Arabian` object is seen in the `Animals` collection, it qualifies both as an `Animal` (base type) and as a `Horse` (the known types by the attributes).
Since `Horse` is the more derived type, an `Arabian` will be serialized as a `Horse`.
The serialized discriminator identifies `Horse`, so deserialization creates a `Horse` rather than the more specific `Arabian` type.
Deserialization cannot infer an unregistered runtime type from this payload.

If a `Cat` type is declared that derives directly from `Animal`, it will serialize as an `Animal` and deserialize into an `Animal` object until the `Cat` derived type is added to the attribute list on `Animal`.

## Union case identifiers

Each type in a union is called a union case, and may include the base type itself.
Each of these types have a serializable identifier by which they are recognized during deserialization.

In all the above examples, this identifier was inferred to be @System.Type.Name?displayProperty=nameWithType since none was explicitly given.
For the base type itself, `nil` is the inferred identifier.

These identifiers can be explicitly specified.
This can be useful to maintain backward compatibility with previously serialized data when a type name changes.
Integers can also be assigned as identifiers, which improves performance and reduces the payload size.

String type identifiers are case sensitive.

The following example shows explicitly choosing the string identifiers:

[!code-csharp[](../../samples/cs/Unions.cs#StringAliasTypes)]

Or we can use the more performant integer identifiers:

[!code-csharp[](../../samples/cs/Unions.cs#IntAliasTypes)]

Mixing identifier types for a given base type is allowed, as shown here:

[!code-csharp[](../../samples/cs/Unions.cs#MixedAliasTypes)]

Note that while inferrence is the simplest syntax, it results in the serialized schema including the name of the type, which can break the schema if the type is renamed.

### Unrecognized identifiers

By default, an unrecognized union discriminator causes a <xref:Nerdbank.MessagePack.MessagePackSerializationException> during deserialization.

You can opt in to preserving an unrecognized union case when the union base type is concrete and declares a readable and writable <xref:Nerdbank.MessagePack.UnusedDataPacket> member.
In that case, the payload is deserialized as the base type.
The raw discriminator and properties that are not known on the base type are retained in the <xref:Nerdbank.MessagePack.UnusedDataPacket> so that reserializing the object writes them back out.
This allows an older version to round-trip a newer union case without knowing or instantiating its derived CLR type.
When a newer version that recognizes the discriminator deserializes the reserialized data, it can create the derived type normally.

Note that a missing derived type *might* have had special behaviors even relating to properties declared on the base type.
For example, if the derived type had business rules that forbade base property Foo from being set to `true` while derived property Bar was set to `true`, those rules would not be enforced when deserializing as the base type.
This potentially allows the business rules to be violated when the older process changes the base property to `true` while the derived property remains `true`.
As always, when deserializing data from an untrusted source, exercise caution and validate the data appropriately.

This preserves the discriminator and unrecognized properties, but is not a byte-for-byte guarantee: properties known to the base type are reserialized using the local serializer configuration.
For more information about <xref:Nerdbank.MessagePack.UnusedDataPacket>, see [customizing serialization](customizing-serialization.md).

## Union serialization format

By default, unions are serialized as 2-element arrays, where the first element is the type identifier (discriminator) and the second element is the object data:

```json
["TypeName", {"property": "value"}]
```

For interoperability with other MessagePack libraries that use different union conventions, you can configure the serializer to use an object-based format instead:

[!code-csharp[](../../samples/cs/Unions.cs#UseDiscriminatorObjects)]

With this setting enabled, unions are serialized as objects with a single property, where the property name is the type identifier:

```json
{"TypeName": {"property": "value"}}
```

Both formats support string and integer discriminators. The setting affects both serialization and deserialization, so it must be consistently applied across all systems that communicate using the same MessagePack protocol.

## Generic derived types

<xref:PolyType.DerivedTypeShapeAttribute> may reference closed generic types, with all type arguments specified.
Each union case must have a unique identifier, which can be assigned explicitly:

[!code-csharp[](../../samples/cs/Unions.cs#ClosedGenericSubTypes)]

Open generic types are also supported with PolyType 1.4.1 or later, provided their type arguments can be inferred from the base type:

[!code-csharp[](../../samples/cs/Unions.cs#OpenGenericSubTypes)]

For `Base<int>`, the registered union case resolves to `Derived<int>`.

## C# union types

C# 15 introduces [union types](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md), which allow a value to be exactly one of a closed set of case types, even when those types do not share a common base type.
Nerdbank.MessagePack serializes C# union types with no additional attributes beyond those that you already use to generate type shapes:

[!code-csharp[](../../samples/cs/Unions.cs#CSharpUnion)]

A `Pet` round-trips like any other type, using the same union schema described above:

[!code-csharp[](../../samples/cs/Unions.cs#CSharpUnionUsage)]

C# union types have a few characteristics that differ from type hierarchies:

* Native scalar cases (`bool`, `string`, `char`, the eight 8- through 64-bit integer types, `float`, and `double`) serialize without a discriminator.
  For example, `union Result(int, string)` containing `42` serializes simply as `42`, and a string payload serializes simply as a MessagePack string.
* Other cases, including objects and nested unions, use the discriminator format described above.
  The default identifier is the case type's name, or its inferred integer tag when <xref:Nerdbank.MessagePack.MessagePackSerializer.PerfOverSchemaStability> is `true`.
  Renaming a tagged case changes its default string identifier, and reordering or inserting cases changes inferred integer identifiers.
* A [custom identifier](#custom-c-union-case-identifiers) always forces a discriminator, even for a primitive or collection case.
  Custom payload converters also retain discriminators because their wire representation is not necessarily the type's native representation.
* Null payloads follow their case's encoding: a null string is bare `nil`, while a null object payload retains its discriminator.
  For example, `union Result(int, string?)` holding `null` serializes as `nil`, while a null `Apple` case serializes as `["Apple", null]`.
  Selection of a null case on serialization follows the type shape; C# unions that do not retain null case identity select the first nullable case.
  For reference-typed unions, a null payload always retains its discriminator so that a non-null union instance is distinguishable from a null union reference, which uses bare `nil`.
* A union value that doesn't match any case (such as `default` for a struct union with no nullable case) cannot be serialized.
* Union types can be recursive, such as `union Tree(int, Tree[])`.
* The [union serialization format](#union-serialization-format) options apply to tagged C# union cases.

When a union has several unaliased native integer cases, each is written using its exact-width, signed or unsigned MessagePack encoding.
For example, `union Number(int, short)` writes an `int` containing `8` as an `int32` and a `short` containing `8` as an `int16`, preserving the original case.
With just one unaliased native integer case, the shortest encoding is used instead.
On deserialization, a matching integer encoding takes precedence; otherwise, the smallest case that can represent the value is chosen, preferring signed integers when their widths are equal.
Thus, a positive fixint containing `8` selects `short` in `union Number(int, short)`.
The native `float32` and `float64` encodings distinguish `float` and `double` cases without additional headers.
Cases such as `char` and `ushort` have the same native integer encoding and require an explicit alias on at least one case.

Collections also serialize without a discriminator, including binary byte collections.
Converter construction rejects more than one untagged collection case, even if the collection kinds differ.
Multiple collection cases are supported when you assign [explicit discriminators](#custom-c-union-case-identifiers) to enough of them that at most one collection case remains untagged.
For example, a union containing an array and a list is supported if either or both collection cases have explicit discriminators.
When several untagged reference cases can serialize as `nil`, such as `string` and an array, a naked `nil` selects the first such case in declaration order.
Cases with explicit discriminator mappings do not participate in this selection because their null payloads require a discriminator.
Assign explicit aliases when preserving the null case's identity is important.

When reference preservation is enabled for a reference-typed union, reference-type payload cases retain their discriminators.
This keeps references to payload objects distinguishable from references to the containing union instances.

C# unions automatically select a discriminator wrapper that cannot be confused with an untagged collection case.
An untagged array case selects map discriminator wrappers, while an untagged dictionary case selects array discriminator wrappers.
This choice overrides <xref:Nerdbank.MessagePack.MessagePackSerializer.UseDiscriminatorObjects> for that union and applies to reading, writing, and generated schemas, including cases with explicit aliases.
Without an untagged array or dictionary case, the configured wrapper format is retained.
Binary collections do not conflict with either wrapper format.

The wrapper selection depends on the declared cases, not the current value.
Adding or removing an untagged collection case can therefore change the wire format of the union's tagged cases.
For a stable protocol, explicitly tag the collection case and configure the desired wrapper format.

Untagged object payloads are not inferred by duck typing.
This C# union encoding does not change derived-type or F# union serialization.

Types that implement the C# union member pattern without using the `union` keyword are also supported.

> [!NOTE]
> The `union` samples above require C# 15 or later.

### Custom C# union case identifiers

Set <xref:Nerdbank.MessagePack.MessagePackSerializer.UnionTypeAliasProvider> to assign stable string or integer identifiers to C# union cases.
This does not change which cases belong to a union or the payload's own converter.
Providing an identifier forces that case to use a discriminator wrapper, including primitive and collection cases that would otherwise be untagged.
It does not affect type hierarchy or F# unions.

#### Explicit aliases for primitive cases

Start with a union over `string` and `int`.
Use <xref:Nerdbank.MessagePack.UnionCaseMapping> to give these otherwise untagged cases the identifiers `text` and `number`:

[!code-csharp[](../../samples/cs/UnionCaseAliases.cs#PrimitiveUnionCaseAliases)]

No interface or common base type is required.
The aliases can also be integers: for example, replace `"text"` and `"number"` in the registrations with `1` and `2`.
The same payload type can have different identifiers in different unions.
No type shape provider or inheritance relationship is required for registration.
<xref:Nerdbank.MessagePack.UnionCaseMappingCollection> implements <xref:Nerdbank.MessagePack.IUnionTypeAliasProvider> and freezes each mapping during construction, before it is assigned to a serializer.
Subsequent attempts to add entries to those mappings throw <xref:System.InvalidOperationException>.
Duplicate union registrations, payload types, and identifiers within a mapping are rejected.
Registered payload types are checked against the union's cases when its converter is constructed.

#### Aliases supplied by static interface members

For more complex cases, your payload types may already expose stable names in code.
For example, error types can implement an interface with a static abstract member:

[!code-csharp[](../../samples/cs/UnionCaseAliases.cs#UnionCaseAliasTypes)]

Rather than repeat those names in the mapping, use a constrained generic registration helper to read each type's static member:

[!code-csharp[](../../samples/cs/UnionCaseAliases.cs#UnionCaseAliasMapping)]

The helper's interface constraint makes `TError.Name` available without reflection.
The mapping still only associates payload types with identifiers; it does not require the union itself to implement the interface.

#### A registry shared by multiple unions

If you already have a registry shared by many unions, use <xref:Nerdbank.MessagePack.DelegateUnionTypeAliasProvider> to look up aliases without registering each union separately:

[!code-csharp[](../../samples/cs/UnionCaseAliases.cs#UnionCaseAliasRegistry)]

The delegate receives both the containing union type and its case's payload type, so it can also resolve union-specific identifiers.
For access to the full union and case shapes, implement <xref:Nerdbank.MessagePack.IUnionTypeAliasProvider> directly.
Providers and delegates must be thread-safe and return consistent results while in use.
Identifiers are resolved during converter construction, not for every serialized value.

Returning `null` from a provider, or leaving a case unregistered in a mapping, preserves the normal encoding for that case, including omission of a discriminator for native scalar and collection cases.
Custom identifiers take precedence over <xref:Nerdbank.MessagePack.MessagePackSerializer.PerfOverSchemaStability>.
They apply to serialization, deserialization, and generated JSON schemas in both union formats.
An overridden case accepts only its custom identifier, not its original name or inferred integer tag.
Identifiers must be unique within a union, including any inferred read tags retained for cases that are not overridden.

## Runtime derived type registration

Static registration via attributes is not always possible.
For instance, you may want to serialize types from a third-party library that you cannot modify.
Or you may have an extensible plugin system where new types are added at runtime.
Or most simply, the derived types may not be declared in the same assembly as the base type, making direct type references for the attributes impossible.

In such cases, runtime registration of derived types is possible to allow you to run any custom logic you may require to discover and register these derived types.
Your code is still responsible to ensure unique identifiers are assigned to each derived type.

Consider the following example where a type hierarchy is registered without using the attribute approach:

# [.NET](#tab/net)

[!code-csharp[](../../samples/cs/Unions.cs#RuntimeSubTypesNET)]

# [.NET Standard](#tab/netfx)

[!code-csharp[](../../samples/cs/Unions.cs#RuntimeSubTypesNETFX)]

---

Runtime derived type registration is no substitute for requiring [type shapes](type-shapes.md).
Rather, runtime derived type registration is useful when:

1. The shapes do not express any derived type information.
1. You need to override the derived type information in the shapes, due to it being incorrect or incomplete (e.g. your application is extensible and you don't have complete derived type information at compile-time).

### Duck typing

Shape-based unions allow you to participate in a union based on the *shape* (the publicly serializable members) of a type rather than requiring a type header.
This enables scenarios sometimes described as "duck typing": if the object "looks like" (matches the required members of) a known union case, it can be deserialized as that case.

Use <xref:Nerdbank.MessagePack.DerivedTypeDuckTyping> to identify a set of types that should be discernable at deserialization time by their shape.

Unions employing the duck typing strategy will deserialize more slowly than typical unions because deserializing the value requires matching the schema of the data against a known union case, while typical unions can immediately identify the target type based on the type identifier.
A good case for duck typing is when protocol compatibility is required, and a union header was not established in the original protocol but multiple types now need to be supported.

Note that duck typing requires there to be enough unique properties between the cases to narrow the possibilities down to exactly one match for deserialization to succeed.

Consider this animal hierarchy:

[!code-csharp[](../../samples/cs/Unions.cs#DuckTyping)]

Serializing a dog or cat might produce this data:

```json
{ "Name": "Rover", "BarkVolume": 10 }
{ "Name": "Whiskers", "MeowPitch": 13 }
```

Note there is no type header added.
When deserializing, the presence of the `BarkVolume` property indicates the object is a `Dog`, while the presence of the `MeowPitch` property indicates a `Cat`.
This works because these respective properties are *required* on their respective types.
Optional properties are *not* considered for duck typing at present.

If two declared types are ambiguous (e.g. if both types have the same required properties), deserialization may fail.

Duck typing is an experimental feature and may change in future releases.
Your feedback is welcome.

### Removing a union at runtime

When a base type has <xref:PolyType.DerivedTypeShapeAttribute> or equivalent attributes on it such that its type shape is recognized as being part of a union, but union behavior is not what you want, you can disable it at runtime by calling <xref:Nerdbank.MessagePack.DerivedTypeUnion.CreateDisabled(System.Type)> and adding it to the serializer configuration.

Taking our farm animals example from earlier, here is how imperative code can disable the union behavior without removing the attributes:

[!code-csharp[](../../samples/cs/Unions.cs#RuntimeSubTypesDisabled)]

## Custom converters

A custom converter applied to the base type in a union brings special considerations.
[Learn about them in our custom converters doc](custom-converters.md#union-types).
