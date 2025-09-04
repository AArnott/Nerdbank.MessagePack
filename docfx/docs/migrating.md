# Migrating from MessagePack-CSharp

If you are migrating from MessagePack-CSharp, or considering doing so, this document is for you.

You should probably start by reviewing the [library feature comparison](features.md#feature-comparison) to make sure that the transition has the possibility of being successful.
If you see a feature is missing from Nerdbank.MessagePack that you need, look for an issue for it and give it a 👍🏻 vote, or file a new issue if you don't see one.

## Migration process

To migrate from MessagePack-CSharp to Nerdbank.MessagePack, begin by adding a package reference to Nerdbank.MessagePack as described in the [Getting Started](getting-started.md) guide.

With the new package referenced, automated code fixes are immediately provided to aid in the mechanics of migration.
You should complete migration before removing references to the old `MessagePack` package so the analyzers and code fixes can do their best work.

The migration analyzers produce diagnostics that are not errors or warnings, so you may need to navigate to an actual piece of code using or implementing a type from MessagePack-CSharp and activate the code fixes menu (<kbd>Ctrl</kbd>+<kbd>.</kbd> or Quick Actions in Visual Studio) to see the migration options.
When you activate the migration code fix, you will have the option to apply the code fix to all occurrences in the solution rather than just the one you found, which can speed up your migration process.

Sometimes after applying one migration code fix, a subsequent analyzer will report new diagnostics, guiding you to the next step in migration.

The following sections demonstrate the changes that are required to migrate from MessagePack-CSharp to Nerdbank.MessagePack.
Remember that automated code fixes can do most or all of this for you.

### `MessagePackObjectAttribute`

MessagePack-CSharp recommends that every user data type be annotated with `[MessagePackObject]` to enable automatic serialization.
In fact unless you use `[MessagePackObject(true)]`, you must also annotate every field or property with `[Key(0)]`, `[Key(1)]`, etc., and members that should *not* be serialized with `[IgnoreMember]`.

With Nerdbank.MessagePack, you can remove the `[MessagePackObject]` attribute from your types, as it is not required.
Nerdbank.MessagePack supports something of a hybrid between MessagePack-CSharp's `[MessagePackObject]` and "contractless" modes, to achieve something much easier to use and yet flexible when you need to tweak it.

Top-level classes and structs that you need to serialize (that is, the ones you pass directly to @Nerdbank.MessagePack.MessagePackSerializer.Serialize* or @"Nerdbank.MessagePack.MessagePackSerializer.Deserialize*") need only that you annotate the type with @PolyType.GenerateShapeAttribute.
Such annotated types *must* be declared with the `partial` modifier to enable source generation to add the necessary serialization code to the type.
Learn more about this in our [Getting Started](getting-started.md) guide.

```diff
-[MessagePackObject]
+[GenerateShape] // only necessary if you pass `MyType` directly to the MessagePackSerializer or KnownSubTypeAttribute
 public class MyType
 {
 }
```

If your type implements `MessagePack.IMessagePackSerializationCallbackReceiver`, you should implement change this to implement @Nerdbank.MessagePack.IMessagePackSerializationCallbacks instead.

#### `KeyAttribute`

Nerdbank.MessagePack also supports @Nerdbank.MessagePack.KeyAttribute, which serves the same function as in MessagePack-CSharp: to change the serialized schema from that of a map of property name=value to an array of values.
Thus, you may keep the `[Key(0)]`, `[Key(1)]`, etc., attributes on your types if you wish to maintain the schema of the serialized data, provided you change the namespace.

If using `[Key("name")]` attributes as a means to change the serialized property names, this must be replaced with @PolyType.PropertyShapeAttribute with @PolyType.PropertyShapeAttribute.Name?displayProperty=nameWithType set to the serialized name.

```diff
-[Key("name")]
+[PropertyShape(Name = "name")]
 public string SomeProperty { get; set; }
```

#### `IgnoreMemberAttribute`

The `[IgnoreMemberAttribute]` that comes from MessagePack-CSharp can be removed from non-public members, which are never considered for serialization by default.
For public members that should be ignored, replace this attribute with @PolyType.PropertyShapeAttribute with @PolyType.PropertyShapeAttribute.Ignore?displayProperty=nameWithType set to `true`.

```diff
-[IgnoreMember]
+[PropertyShape(Ignore = true)]
 public int SomeProperty { get; set; }

-[IgnoreMember]
 internal int AnotherProperty { get; set; }
```

### `UnionAttribute`

MessagePack-CSharp defines a `UnionAttribute` by which you can serialize an object when you know its base type or interface at compile-time, but whose exact type is not known until runtime, provided you can predict the closed set of allowed runtime types in advance.
Nerdbank.MessagePack supports this same use case via its @Nerdbank.MessagePack.KnownSubTypeAttribute, and migration is straightforward:

```diff
-[Union(0, typeof(MyType1))]
-[Union(1, typeof(MyType2))]
+[KnownSubType(typeof(MyType1), 0)]
+[KnownSubType(typeof(MyType2), 1)]
 public interface IMyType
 {
 }
```

Any types referenced by the @Nerdbank.MessagePack.KnownSubTypeAttribute must be annotated with @PolyType.GenerateShapeAttribute as described above.

### `IMessagePackFormatter<T>`

MessagePack-CSharp allows you to define custom formatters for types that it doesn't know how to serialize by default by implementing the `IMessagePackFormatter<T>` interface.
In Nerdbank.MessagePack, that use case is addressed by deriving a class from the @Nerdbank.MessagePack.MessagePackConverter`1 class.
These two APIs are very similar, but the method signatures are slightly different, as well as the patterns that provide security mitigations.

```diff
-public class MyTypeFormatter : IMessagePackFormatter<MyType>
+public class MyTypeFormatter : MessagePackConverter<MyType>
 {
-    public MyType Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
+    public override MyType Read(ref MessagePackReader reader, SerializationContext context)
     {
         if (reader.TryReadNil())
         {
             return null;
         }

         string name = null;
-        options.Security.DepthStep(ref reader);
+        context.DepthStep();
-        try
-        {
             int count = reader.ReadArrayHeader();
             for (int i = 0; i < count; i++)
             {
                 switch (i)
                 {
                     case 0:
-                        name = options.Resolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
+                        name = context.GetConverter<string>().Read(ref reader, context);
                         break;
                     default:
-                        reader.Skip();
+                        reader.Skip(context);
                         break;
                     }
             }

             return new MyType { Name = name };
-        }
-        finally
-        {
-            reader.Depth--;
-        }
     }

-    public void Serialize(ref MessagePackWriter writer, MyType value, MessagePackSerializerOptions options)
+    public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context)
     {
         if (value is null)
         {
             writer.WriteNil();
             return;
         }

         writer.WriteArrayHeader(1);
-        options.Resolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Name, options);
+        context.GetConverter<string>().Write(ref writer, value.Name, context);
     }
 }
```

### `MessagePackFormatterAttribute`

A custom type may be annotated with the `MessagePackFormatterAttribute` to specify a custom formatter for that type.
In Nerdbank.MessagePack, this attribute is replaced with the @Nerdbank.MessagePack.MessagePackConverterAttribute in a straightforward replacement.

```diff
-[MessagePackFormatter(typeof(MyTypeFormatter))]
+[MessagePackConverter(typeof(MyTypeFormatter))]
 public class MyType
 {
     public string Name { get; set; }
 }
```

### Security mitigations

In MessagePack-CSharp, security mitigations are provided by the `MessagePackSecurity` class, as referenced by the `MessagePackSerializerOptions` class.

In Nerdbank.MessagePack, security mitigations are provided by the @Nerdbank.MessagePack.SerializationContext struct, as referenced by @Nerdbank.MessagePack.MessagePackSerializer.StartingContext?displayProperty=nameWithType.

### Incompatibilities

Some functionality in MessagePack-CSharp has no equivalent in Nerdbank.MessagePack, as follows:

- Typeless serialization: MessagePack-CSharp supports serializing and deserializing objects without knowing their type at compile time by serializing the type name after a runtime type check. Deserialization activates an object matching the original type.

  Nerdbank.MessagePack generally requires knowing at least something about the type (see [Unions](unions.md)) at compile time for security reasons and NativeAOT support.
  An [optional `object` converter](xref:Nerdbank.MessagePack.OptionalConverters.WithObjectConverter*) can be used to serialize any runtime type for which a shape is available. It will deserialize into maps, arrays, and primitives rather than the original type.
  [Custom converters](custom-converters.md) can be written to overcome these limitations where required.

### Other API changes

Many APIs are exactly the same or very similar.
In some cases, APIs offering equivalent or similar functionality have been renamed.
To help with migration, the following table lists some of the most common APIs that have changed names.

MessagePack-CSharp | Nerdbank.MessagePack
--- | ---
`ExtensionResult` | @Nerdbank.MessagePack.Extension
`MessagePackReader.ReadExtensionFormat` | @Nerdbank.MessagePack.MessagePackReader.ReadExtension?displayProperty=nameWithType
`MessagePackReader.ReadExtensionFormatHeader` | @Nerdbank.MessagePack.MessagePackReader.ReadExtensionHeader?displayProperty=nameWithType
`MessagePackWriter.WriteExtensionFormat` | @Nerdbank.MessagePack.MessagePackWriter.Write(Nerdbank.MessagePack.Extension)?displayProperty=nameWithType
`MessagePackWriter.WriteExtensionFormatHeader` | @Nerdbank.MessagePack.MessagePackWriter.Write(Nerdbank.MessagePack.ExtensionHeader)?displayProperty=nameWithType
`IMessagePackSerializationCallbackReceiver` | @Nerdbank.MessagePack.IMessagePackSerializationCallbacks

## Encoding compatibility

MessagePack-CSharp and Nerdbank.MessagePack both follow standard msgpack encodings [as specified here](https://github.com/msgpack/msgpack/blob/master/spec.md).

Data types that are *not* expressly specified in that spec may vary in their encodings, as described below:

### .NET primitives without a specified encoding

The .NET "primitives" <xref:System.Guid>, <xref:System.Int128>, <xref:System.UInt128>, <xref:System.Decimal>, <xref:System.Numerics.BigInteger> have no specified encoding in msgpack.

Nerdbank.MessagePack can *read* all these types as MessagePack-CSharp has written them, but it will default to writing with msgpack extensions for better interoperability.

MessagePack-CSharp writes these values either as strings or as "native" binary using the msgpack Bin header.

Nerdbank.MessagePack uses msgpack extensions with [reassignable type codes](xref:Nerdbank.MessagePack.LibraryReservedMessagePackExtensionTypeCode) for these primitive data types because of the enhanced type information extensions provide.

The following table describes the encodings used by each library.
LE and BE refer to Little Endian and Big Endian, respectively.

Data type | MessagePack-CSharp | Nerdbank.MessagePack
--|--|--
<xref:System.Guid> | 16-byte bin LE or string | 16-byte [Ext](xref:Nerdbank.MessagePack.LibraryReservedMessagePackExtensionTypeCode.Guid) BE or string
<xref:System.Int128> | 16-byte bin LE | int format if it fits, otherwise 16-byte [Ext](xref:Nerdbank.MessagePack.LibraryReservedMessagePackExtensionTypeCode.Int128) BE
<xref:System.UInt128> | 16-byte bin LE | int format if it fits, otherwise 16-byte [Ext](xref:Nerdbank.MessagePack.LibraryReservedMessagePackExtensionTypeCode.UInt128) BE
<xref:System.Decimal> | 16-byte bin LE, [MS-OAUT 2.2.26 DECIMAL](https://learn.microsoft.com/openspecs/windows_protocols/ms-oaut/b5493025-e447-4109-93a8-ac29c48d018d) | 16-byte [Ext](xref:Nerdbank.MessagePack.LibraryReservedMessagePackExtensionTypeCode.Decimal) LE, [MS-OAUT 2.2.26 DECIMAL](https://learn.microsoft.com/openspecs/windows_protocols/ms-oaut/b5493025-e447-4109-93a8-ac29c48d018d)
<xref:System.Numerics.BigInteger> | Bin LE with twos-complement bytes, using the fewest number of bytes possible | int format if it fits, otherwise [Ext](xref:Nerdbank.MessagePack.LibraryReservedMessagePackExtensionTypeCode.BigInteger) BE with twos-complement bytes, using the fewest number of bytes possible

### .NET classes and structs with members

Nerdbank.MessagePack can read MessagePack-CSharp serialized objects, but MessagePack-CSharp cannot read some of the objects serialized by Nerdbank.MessagePack.

MessagePack-CSharp and Nerdbank.MessagePack can represent complex types (i.e. classes and structs with fields and/or properties) as either msgpack maps (with property names and values) or msgpack arrays (with values in array indexes assigned by attribute).

Consider the following user-defined type:

```cs
[MessagePackObject(true)] // only required by MessagePack-CSharp
public class Foo
{
    public string Bar { get; }
}
```

Both libraries would serialize this as a map, which would look like this if rendered in JSON:

```json
{ "Bar": "some value" }
```

Now consider the following variant of that class:

```cs
[MessagePackObject] // only required by MessagePack-CSharp
public class Foo
{
    [Key(0)]
    public string Bar { get; }
}
```

Both libraries would serialize this as a array, which would look like this if rendered in JSON:

```json
["some value"]
```

Nerdbank.MessagePack can automatically assign indexes for all properties when <xref:Nerdbank.MessagePack.MessagePackSerializer.PerfOverSchemaStability> is set to `true`, causing the more compact and performant array encoding to be used instead, without the overhead of maintaining <xref:Nerdbank.MessagePack.KeyAttribute> on all serialized members.

Nerdbank.MessagePack has the unique ability to optimize the array representation for space when the arrays would have many 'holes' in them by using maps where the array indexes are used as keys in the map, which might look something like this:

```json
{
    0: "some value",
    5: "another value"
}
```

Note that while an integer key is illegal in JSON, it is perfectly legal and efficient in msgpack.

For the same object, MessagePack-CSharp would have emitted the larger:

```json
["some value",null,null,null,null,"another value"]
```
