# Unions

## Polymorphic serialization

You can serialize instances of certain types derived from the declared type and deserialize them back to their original runtime types using the `KnownSubTypeAttribute`.

For instance, suppose you have this type to serialize:

```cs
public class Farm
{
    public List<Animal> Animals { get; set; }
}
```

But there are many kinds of animals.
You can get them to serialize and deserialize correctly like this:

```cs
[KnownSubType(1, typeof(Cow))]
[KnownSubType(2, typeof(Horse))]
[KnownSubType(3, typeof(Dog))]
public class Animal
{
    public string Name { get; set; }
}

public class Cow : Animal { }
public class Horse : Animal { }
public class Dog : Animal { }
```

This changes the schema of the serialized data to include a tag that indicates the type of the object.

*Without* any `KnownSubTypeAttribute`, an `Animal` object would serialize like this (as represented in JSON):

```json
{ "Name": "Bessie" }
```

But with the `KnownSubTypeAttribute`, it serializes like this:
```json
[null, { "Name": "Bessie" }]
```

See how the natural form of `Animal` is still there, but nested as the second element in a 2-element array.
The `null` first element indicates that the object was literally `Animal` (rather than a derived type).
If the serialized object were an instance of `Cow`, the first element would be `1` instead of `null`:

```json
[1, { "Name": "Bessie" }]
```

This special union schema is only used when the statically *declared* type is a class that has `KnownSubTypeAttribute` on it.
It is *not* used when the derived type is statically known. For example, consider this collection of horses:

```cs
public class HorsePen
{
    public List<Horse> Horses { get; set; }
}
```

This would serialize like this:

```json
{ "Horses": [{ "Name": "Bessie" }] }
```

Note the lack of the union schema that would add `[2, ... ]` around every horse.
This is because the `Horse` type is statically known as the generic type argument of the collection, and there's no need to add serialized data to indicate the runtime type.

Now suppose you have different breeds of horses that each had their own subtype:

```cs
[KnownSubType(1, typeof(QuarterHorse))]
[KnownSubType(2, typeof(Thoroughbred))]
public class Horse : Animal { }

public class QuarterHorse : Horse { }
public class Thoroughbred : Horse { }
```

At this point your `HorsePen` *would* serialize with the union schema around each horse:
```json
{ "Horses": [[1, { "Name": "Bessie" }], [2, { "Name", "Lightfoot" }]] }
```

But now let's consider your `Farm` class, which has a collection of `Animal` objects.
The `Animal` class only knows about `Horse` as a subtype and designates `2` as the alias for that subtype.
`Animal` has no designation for `QuarterHorse` or `Thoroughbred`.
As such, serializing your `Farm` would drop any details about horse breeds and deserializing would produce `Horse` objects, not `QuarterHorse` or `Thoroughbred`.
To fix this, you would need to add `KnownSubTypeAttribute` to the `Animal` class for `QuarterHorse` and `Thoroughbred` that assigns type aliases for each of them.
