# Surrogate types

While using the @PolyType.GenerateShapeAttribute is by far the simplest way to make an entire type graph serializable, some types may not be compatible with automatic serialization.
In such cases, you can define a surrogate type that _is_ serializable and a marshaler that can convert between the two types.

Surrogate types are an easier way to make an unserializable type serializable than writing [custom converters](custom-converters.md).
Surrogate types can also effectively assist with the [structural equality](structural-equality.md) feature for types that may not be directly comparable.

Suppose you have the following type, which has fields that are not directly serializable.
This could be because the fields are of a type that cannot be directly serialized.
In this sample they are private fields which are not serialized by default (though they could be with an attribute, but we're ignoring that for purposes of this sample).

[!code-csharp[](../../samples/cs/SurrogateTypes.cs#OnlyOriginalType)]

To serialize this type, we'll use a surrogate that _does_ expose these fields for serialization.

## Write a surrogate type

Surrogate types should generally be structs to avoid allocation costs from these temporary conversions.
They can be quite simple, containing only the properties necessary to enable automatic serialization.
This `record struct` is the simplest syntax for expressing public properties for serialization.
We've chosen properties that correspond to the fields in the previous sample that require serialization for the surrogate type defined here.

[!code-csharp[](../../samples/cs/SurrogateTypes.cs#SurrogateType)]

The surrogate must have at least `internal` visibility.

## Write a marshaler

Now we need to define a simple marshaler that can copy the data from the non-serializable type to its surrogate, and back again.
The marshaler implements <xref:PolyType.IMarshaler`2>.

> [!IMPORTANT]
> When the original type is a reference type and the surrogate type is a value type, make sure to specify a _nullable_ surrogate type so that your marshaler can retain the `null` identity properly.

When this marshaler is _nested_ within the original type, C# gives it access to the containing type's private fields, which is useful for this sample.

[!code-csharp[](../../samples/cs/SurrogateTypes.cs#Marshaler)]

The marshaler must have at least `internal` visibility.

This marshaler must be referenced via <xref:PolyType.TypeShapeAttribute.Marshaler?displayProperty=nameWithType> on an attribute applied to the original type.

## Sample

Taken together with the added @PolyType.TypeShapeAttribute that refers to the marshaler, we have the following complete sample:

[!code-csharp[](../../samples/cs/SurrogateTypes.cs#CompleteSample)]

### Surrogates for external types

When the unserializable type is declared in an assembly you don't control, such that you cannot add a <xref:PolyType.TypeShapeExtensionAttribute> to the type declaration, you can use a type shape *extension* to achieve the same end:

```cs
[assembly: TypeShapeExtension(typeof(SomeExternalType), Marshaler = typeof(MyMarshalerForSomeExternalType))]
```

### Open generic data type

Open generic data types can define surrogates for themselves as well.
Just take care to use the generic type definition syntax (no type arguments specified) when referencing the surrogate type.

[!code-csharp[](../../samples/cs/SurrogateTypes.cs#OpenGeneric)]

While the @PolyType.GenerateShapeAttribute cannot be applied to an open generic data type,
this data type can be closed and used from another data structure.
It can also be used as the top-level structure by closing the generic on a Witness class.

# [.NET](#tab/net)

[!code-csharp[](../../samples/cs/SurrogateTypes.cs#ClosedGenericViaWitnessNET)]

# [.NET Standard](#tab/netfx)

[!code-csharp[](../../samples/cs/SurrogateTypes.cs#ClosedGenericViaWitnessNETFX)]

---
