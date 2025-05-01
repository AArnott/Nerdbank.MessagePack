# Security

Security considerations come into play especially when deserializing data from an untrusted source.
Vulnerabilities typically include denial of service attacks at the deserializer level itself but may involve far more damaging vulnerabilities depending on how the deserialized data is later used by an application.

In this topic, we will focus on the vulnerabilities that are specific to the deserialization layer.

## Stack overflows

A very simple attack to carry out is crashing the deserializing process by forcing the deserializer to "stack overflow".
With each nested structure (e.g. a map or array) in msgpack, another frame is added to the deserializing thread's "stack".
This stack space is limited and if exceeded, the process will crash.
It can take less than 500 bytes of msgpack to crash a deserializing application that does not guard against such stack overflows.

Nerdbank.MessagePack protects against such attacks by artificially limiting the level of nesting that is allowed before a deserializer will throw an exception that can be caught and processed by an application rather than crash it.
This limit is set by @Nerdbank.MessagePack.SerializationContext.MaxDepth, which by default is set to a conservative default value that should prevent stack overflows.
When the data to be deserialized has a legitimate need for deeper nesting than the default limit allows, this limit may be adjusted, like this:

[!code-csharp[](../../samples/cs/Security.cs#SetMaxDepth)]

## Hash collisions

When deserializing data into a dictionary or any other collection that hashes a key to provide fast lookup times, an adversary may carefully choose the keys in the collection such that when deserialized, the hash codes will collide, reducing the performance of the collection from its typical O(1) or "constant time" performance to O(n) or "linear time" performance.
Such a performance degradation can be dramatic, particularly when combined with an application's other algorithms that may multiply this effect.
This is a very cheap way for an adversary to bring the application to a crawl, leading to a denial of service to other innocent users.

To defend against this threat while deserializing untrusted data, it is important to use collision resistant hash functions for the keys in your collections.
Doing so dramatically increases the cost to the attacker to carry out this attack and should severely limit the impact they can have on your service.

> [!IMPORTANT]
> The collections types included with .NET do _not_ use collision resistant hash functions by default except for @System.String specifically when it is used as a key.
> .NET does not supply collision resistant hash functions to use for any other type.
> The @System.HashCode type in particular does _not_ offer collision resistance.
> They must come from your own code or a library with cryptographic hash functions.

This library does not (yet) have the capability to create collections during deserialization that have collision resistant hash functions, due to [a limitation in PolyType](https://github.com/eiriktsarpalis/PolyType/issues/33), which it depends on.

Instead, you can provide your own defense by initializing your collections with a collision resistant implementation of @System.Collections.Generic.IEqualityComparer`1 in your data type's constructor.

> [!NOTE]
> Hash collision resistance has no impact on the serialized data itself.
> A program may defend itself against hash collision attacks without breaking interoperability with other parties that they exchange data with.

Here is an example of a defense against hash collisions:

# [.NET](#tab/net)

[!code-csharp[](../../samples/cs/Security.cs#SecureEqualityComparersNET)]

# [.NET Standard](#tab/netfx)

[!code-csharp[](../../samples/cs/Security.cs#SecureEqualityComparersNETFX)]

---

Note how the collection properties do _not_ define a property setter.
This is crucial to the threat mitigation, since it activates the deserializer behavior of not recreating the collection using the default (insecure) equality comparer.

In this example, we use @Nerdbank.MessagePack.StructuralEqualityComparer.GetHashCollisionResistant*?displayProperty=nameWithType, which provides a collision resistant implementation of @System.Collections.Generic.IEqualityComparer`1.
This implementation uses the SIP hash algorithm, which is known for its high performance and collision resistance.
While it will function for virtually any data type, its behavior is not correct in all cases and you may need to implement your own secure hash function.
Please review the documentation for @Nerdbank.MessagePack.StructuralEqualityComparer.GetHashCollisionResistant* for more information.

## Multiple values for the same property

Attackers will sometimes attempt to exploit vulnerabilities in a system by providing multiple values for the same property.
Consider this JSON object:

```json
{ "accessRequested": "guest", "accessRequested": "admin" }
```

If this object represents a request and was received and checked for necessary permissions before the being forwarded to a processor, there's a potential exploit.
If the permission check only scans for the first definition of the `accessRequested` property and sees that `guest` permission is requested, it may approve and forward the request to the processor.
The processor may need to understand the whole object and therefore fully deserialize it.
If the deserializer is implemented as most are, the last value given for a property may be the one last applied to the deserialized object.
This means that although the security check saw "guest", the processor will see "admin".

There is no good reason for a serialized object to define two values for the same property.
The same exploit is possible with objects encoded in messagepack.
Nerdbank.MessagePack mitigates this threat automatically by throwing a <xref:Nerdbank.MessagePack.MessagePackSerializationException> with its <xref:Nerdbank.MessagePack.MessagePackSerializationException.Code> property set to <xref:Nerdbank.MessagePack.MessagePackSerializationException.ErrorCode.DoublePropertyAssignment> during deserialization when any such double assignment is detected.
