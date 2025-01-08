# By-value equality testing

.NET provides reference types with a default implementation of @System.Object.GetHashCode?displayProperty=nameWithType and @System.Object.Equals(System.Object)?displayProperty=nameWithType that considers every object to be unique, and therefore these methods only return 'equivalent' results when two references are to the same actual object.
Types may override these methods to provide by-value equality and hash functions, and in fact are encouraged to do so.
@System.Collections.Generic.EqualityComparer`1.Default?displayProperty=nameWithType in fact relies on this methods to perform its function.

Even when types override these methods, they are often not implemented for _deep_ by-value comparison.
This is particularly true when a type contains collection members, since testing a collection's contents for by-value equality of each element can be difficult.

Nerdbank.MessagePack alleviates these difficulties somewhat by providing deep, by-value equality testing and hashing for arbitrary types, using the same @PolyType.GenerateShapeAttribute technology that it uses for serialization.
It does this via the @Nerdbank.MessagePack.ByValueEqualityComparer.GetDefault\*?displayProperty=nameWithType method, which returns an instance of @System.Collections.Generic.IEqualityComparer`1 for the specified type that provides deep, by-value checking and hashing.

Here is an example of using this for by-value equality checking for a user-defined type that does not implement it itself:

# [.NET](#tab/net)

[!code-csharp[](../../samples/ByValueEquality.cs#ByValueEqualityNET)]

# [.NET Standard](#tab/netfx)

[!code-csharp[](../../samples/ByValueEquality.cs#ByValueEqualityNETFX)]

---

Collision resistant hashing functions can be produced by calling @Nerdbank.MessagePack.ByValueEqualityComparer.GetHashCollisionResistant* instead of @Nerdbank.MessagePack.ByValueEqualityComparer.GetDefault*, as described in [our topic on hash collisions](security.md#hash-collisions).

Learn more from @Nerdbank.MessagePack.ByValueEqualityComparer.
