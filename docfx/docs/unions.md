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

You can preserve polymorphic type metadata across serialization using the @PolyType.DerivedTypeShapeAttribute, which you apply to the type that is used as the declared base type.
Since `Animal` is used as the collection element type, we apply the attributes on the declaraiton of that type:

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

If the `Horse` class lacked any @PolyType.DerivedTypeShapeAttribute of its own, serializing your `Farm` would drop any details about horse breeds and deserializing would produce `Horse` objects where the original object graph may have contained `QuarterHorse` or `Thoroughbred`.
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

When a derived type is not listed on its base union type, its nearest listed base type is recognized instead.
For example, consider our flattened `Animal` class definition given earlier, where `Horse` and its two derived types are documented via attributes on the `Animal` class.
Now suppose we declare a new `Horse`-derived type called `Arabian`, but we omit adding an attribute for that derived type on `Animal`.
When an `Arabian` object is seen in the `Animals` collection, it qualifies both as an `Animal` (base type) and as a `Horse` (the known types by the attributes).
Since `Horse` is the more derived type, an `Arabian` will be serialized as a `Horse`.
When deserialized, this object will be rehydrated as a `Horse` rather than as its more specific `Arabian` type.

If a `Cat` type is declared that derives directly from `Animal`, it will serialize as an `Animal` and deserialize into an `Animal` object until the `Cat` derived type is added to the attribte list on `Animal`.

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

## Generic derived types

@PolyType.DerivedTypeShapeAttribute may reference generic derived types, but they must be *closed* generic types (i.e. all the generic type arguments must be specified).
You may close the generic type several times, but each one needs a unique type identifier so the inferred type name will not work.
You will have to explicitly specify them.

[!code-csharp[](../../samples/cs/Unions.cs#ClosedGenericSubTypes)]

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
