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

```cs
var serializer = new MessagePackSerializer
{
    StartingContext = new SerializationContext
    {
        MaxDepth = 100,
    },
};
```

## Hash collisions

When deserializing data into a dictionary or any other collection that hashes a key to provide fast lookup times, an adversary may carefully choose the keys in the collection such that when deserialized, the hash codes will collide, reducing the performance of the collection from its typical O(1) or "constant time" performance to O(n) or "linear time" performance.
Such a performance degradation can be dramatic, particularly when combined with an application's other algorithms that may multiply this effect.
This is a very cheap way for an adversary to bring the application to a crawl, leading to a denial of service to other innocent users.

To defend against this threat while deserializing untrusted data, it is important to use collision resistant hash functions for the keys in your collections.
Doing so dramatically increases the cost to the attacker to carry out this attack and should severely limit the impact they can have on your service.

> [!IMPORTANT]
> The collections types included with .NET do *not* use collision resistant hash functions by default except for @System.String specifically when it is used as a key.
> .NET does not supply collision resistant hash functions to use for any other type.
> The @System.HashCode type in particular does *not* offer collision resistance.
> They must come from your own code or a library with cryptographic hash functions.

This library does not (yet) have the capability to create collections during deserialization that have collision resistant hash functions, due to [a limitation in TypeShape](https://github.com/eiriktsarpalis/typeshape-csharp/issues/33), which it depends on.