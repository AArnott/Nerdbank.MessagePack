# Migrating from MessagePack-CSharp

If you are migrating from MessagePack-CSharp, or considering doing so, this document is for you.

You should probably start by reviewing the features of each library to make sure that the transition has the possibility of being successful.
If you see a feature is missing from ShapeShift that you need, look for an issue for it and give it a 👍🏻 vote, or file a new issue if you don't see one.

## Feature comparison

See how this library compares to other .NET MessagePack libraries.

In many cases, the ✅ or ❌ in the table below are hyperlinks to the relevant documentation or an issue you can vote up to request the feature.

| Feature                                                             |                             ShapeShift                             |                                                          MessagePack-CSharp                                                           |
| ------------------------------------------------------------------- | :----------------------------------------------------------------: | :-----------------------------------------------------------------------------------------------------------------------------------: |
| Optimized for high performance                                      |                        [✅](performance.md)                        |                     [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#performance)                     |
| Contractless data types                                             |                    [✅](getting-started.md)[^1]                    |                [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#object-serialization)                 |
| Attributed data types                                               |                 [✅](customizing-serialization.md)                 |                [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#object-serialization)                 |
| Polymorphic serialization                                           |                          [✅](unions.md)                           |                      [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#union)[^4]                      |
| Typeless serialization                                              |                                 ❌                                 |                      [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#typeless)                       |
| Skip serializing default values                                     | [✅](xref:ShapeShift.SerializerBase.SerializeDefaultValues)        |                               [❌](https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/678)                               |
| Dynamically use maps or arrays for most compact format              |          [✅](customizing-serialization.md#array-or-map)           |                              [❌](https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/1953)                               |
| Surrogate types for automatic serialization of unserializable types |                      [✅](surrogate-types.md)                      |                                                                  ❌                                                                   |
| Custom converters                                                   |                     [✅](custom-converters.md)                     |                                                                  ✅                                                                   |
| Stateful converters                                                 |           [✅](custom-converters.md#stateful-converters)           |                                                                  ❌                                                                   |
| Deserialization callback                                            |      [✅](xref:ShapeShift.ISerializationCallbacks)                 |               [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#serialization-callback)                |
| MsgPack extensions                                                  |                                 ✅                                 |                                                                  ✅                                                                   |
| LZ4 compression                                                     |       [❌](https://github.com/AArnott/ShapeShift/issues/34)        |                   [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#lz4-compression)                   |
| Trim-safe                                                           |                                 ✅                                 |                                                                  ❌                                                                   |
| NativeAOT ready                                                     |                                 ✅                                 |                                                                ❌[^2]                                                                 |
| Unity                                                               |                         [✅](unity.md)[^3]                         |                    [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#unity-support)                    |
| Async                                                               |    [✅](xref:ShapeShift.SerializerBase.SerializeAsync*)            |                                                                  ❌                                                                   |
| Endless streaming deserialization                                   |                 [✅](streaming-deserialization.md)                 | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp/?tab=readme-ov-file#multiple-messagepack-structures-on-a-single-stream) |
| Reference preservation                                              |   [✅](xref:ShapeShift.MessagePackSerializer.PreserveReferences)   |                                                                  ❌                                                                   |
| JSON schema export                                                  |     [✅](xref:ShapeShift.SerializerBase.GetJsonSchema*)            |                                                                  ❌                                                                   |
| Secure defaults                                                     |                                 ✅                                 |                                                                  ❌                                                                   |
| Automatic hash collection deserialization in secure mode            |                                 ❌                                 |                                                                  ✅                                                                   |
| Automatic collision-resistant hash function for arbitrary types     |          [✅](xref:ShapeShift.StructuralEqualityComparer)          |                                                                  ❌                                                                   |
| Free of mutable statics                                             |                                 ✅                                 |                                                                  ❌                                                                   |
| Structural `IEqualityComparer<T>` for arbitrary types               |                                 ✅                                 |                                                                  ❌                                                                   |

Security is a complex subject, and an area where ShapeShift is actively evolving.
[Learn more about how to secure your deserializer](security.md).

[^1]: ShapeShift's approach is more likely to be correct by default and more flexible to fixing when it is not.
[^2]: Although MessagePack-CSharp does not support .NET 8 flavor NativeAOT, it has long-supported Unity's il2cpp runtime, but it requires careful avoidance of dynamic features.
[^3]: Particular steps are currently required, and limitations apply. See our [unity doc](unity.md) for more information.
[^4]: MessagePack-CSharp is limited to derived types that can be attributed on the base type, whereas ShapeShift allows for dynamically identifying derived types at runtime.

## Migration process

To migrate from MessagePack-CSharp to ShapeShift, begin by adding a package reference to ShapeShift as described in the [Getting Started](getting-started.md) guide.

With the new package referenced, automated code fixes are immediately provided to aid in the mechanics of migration.
You should complete migration before removing references to the old `MessagePack` package so the analyzers and code fixes can do their best work.

The migration analyzers produce diagnostics that are not errors or warnings, so you may need to navigate to an actual piece of code using or implementing a type from MessagePack-CSharp and activate the code fixes menu (<kbd>Ctrl</kbd>+<kbd>.</kbd> or Quick Actions in Visual Studio) to see the migration options.
When you activate the migration code fix, you will have the option to apply the code fix to all occurrences in the solution rather than just the one you found, which can speed up your migration process.

Sometimes after applying one migration code fix, a subsequent analyzer will report new diagnostics, guiding you to the next step in migration.

The following sections demonstrate the changes that are required to migrate from MessagePack-CSharp to ShapeShift.
Remember that automated code fixes can do most or all of this for you.

### `MessagePackObjectAttribute`

MessagePack-CSharp recommends that every user data type be annotated with `[MessagePackObject]` to enable automatic serialization.
In fact unless you use `[MessagePackObject(true)]`, you must also annotate every field or property with `[Key(0)]`, `[Key(1)]`, etc., and members that should _not_ be serialized with `[IgnoreMember]`.

With ShapeShift, you can remove the `[MessagePackObject]` attribute from your types, as it is not required.
ShapeShift supports something of a hybrid between MessagePack-CSharp's `[MessagePackObject]` and "contractless" modes, to achieve something much easier to use and yet flexible when you need to tweak it.

Top-level classes and structs that you need to serialize (that is, the ones you pass directly to @ShapeShift.SerializerBase.Serialize* or @"ShapeShift.SerializerBase.Deserialize*") need only that you annotate the type with @PolyType.GenerateShapeAttribute.
Such annotated types _must_ be declared with the `partial` modifier to enable source generation to add the necessary serialization code to the type.
Learn more about this in our [Getting Started](getting-started.md) guide.

```diff
-[MessagePackObject]
+[GenerateShape] // only necessary if you pass `MyType` directly to the MessagePackSerializer or KnownSubTypeAttribute
 public class MyType
 {
 }
```

If your type implements `MessagePack.IMessagePackSerializationCallbackReceiver`, you should implement change this to implement @ShapeShift.ISerializationCallbacks instead.

#### `KeyAttribute`

ShapeShift also supports @ShapeShift.KeyAttribute, which serves the same function as in MessagePack-CSharp: to change the serialized schema from that of a map of property name=value to an array of values.
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
ShapeShift supports this same use case via its @ShapeShift.KnownSubTypeAttribute, and migration is straightforward:

```diff
-[Union(0, typeof(MyType1))]
-[Union(1, typeof(MyType2))]
+[KnownSubType(typeof(MyType1), 0)]
+[KnownSubType(typeof(MyType2), 1)]
 public interface IMyType
 {
 }
```

Any types referenced by the @ShapeShift.KnownSubTypeAttribute must be annotated with @PolyType.GenerateShapeAttribute as described above.

### `IMessagePackFormatter<T>`

MessagePack-CSharp allows you to define custom formatters for types that it doesn't know how to serialize by default by implementing the `IMessagePackFormatter<T>` interface.
In ShapeShift, that use case is addressed by deriving a class from the @ShapeShift.Converters.Converter`1 class.
These two APIs are very similar, but the method signatures are slightly different, as well as the patterns that provide security mitigations.

```diff
-public class MyTypeFormatter : IMessagePackFormatter<MyType>
+public class MyTypeFormatter : Converter<MyType>
 {
-    public MyType Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
+    public override MyType Read(ref Reader reader, SerializationContext context)
     {
-        if (reader.TryReadNil())
+        if (reader.TryReadNull())
         {
             return null;
         }

         string name = null;
-        options.Security.DepthStep(ref reader);
+        context.DepthStep();
-        try
-        {
-            int count = reader.ReadArrayHeader();
+            int count = reader.ReadStartVector();
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
+    public override void Write(ref Writer writer, in MyType value, SerializationContext context)
     {
         if (value is null)
         {
-            writer.WriteNil();
+            writer.WriteNull();
             return;
         }

         writer.WriteStartVector(1);
-        options.Resolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Name, options);
+        context.GetConverter<string>().Write(ref writer, value.Name, context);
     }
 }
```

### `MessagePackFormatterAttribute`

A custom type may be annotated with the `MessagePackFormatterAttribute` to specify a custom formatter for that type.
In ShapeShift, this attribute is replaced with the @ShapeShift.ConverterAttribute in a straightforward replacement.

```diff
-[MessagePackFormatter(typeof(MyTypeFormatter))]
+[Converter(typeof(MyTypeFormatter))]
 public class MyType
 {
     public string Name { get; set; }
 }
```

### Security mitigations

In MessagePack-CSharp, security mitigations are provided by the `MessagePackSecurity` class, as referenced by the `MessagePackSerializerOptions` class.

In ShapeShift, security mitigations are provided by the @ShapeShift.Converters.SerializationContext struct, as referenced by @ShapeShift.SerializerBase.StartingContext?displayProperty=nameWithType.

### Incompatibilities

Some functionality in MessagePack-CSharp has no equivalent in ShapeShift, as follows:

- Typeless serialization: MessagePack-CSharp supports serializing and deserializing objects without knowing their type at compile time.
  ShapeShift requires knowing at least something about the type (see [Unions](unions.md)) at compile time for security reasons and NativeAOT support.
  [Custom converters](custom-converters.md) can be written to overcome these limitations where required.

### Other API changes

Many APIs are exactly the same or very similar.
In some cases, APIs offering equivalent or similar functionality have been renamed.
To help with migration, the following table lists some of the most common APIs that have changed names.

| MessagePack-CSharp                             | ShapeShift                                                                                   |
| ---------------------------------------------- | -------------------------------------------------------------------------------------------- |
| `ExtensionResult`                              | @ShapeShift.MessagePack.Extension                                                            |
| `MessagePackReader.TryReadNil`                 | @ShapeShift.Converters.Reader.TryReadNull?displayProperty=nameWithType        |
| `MessagePackReader.ReadExtensionFormat`        | @ShapeShift.MessagePack.MsgPackDeformatter.ReadExtension(ShapeShift.Converters.Reader@)?displayProperty=nameWithType        |
| `MessagePackReader.ReadExtensionFormatHeader`  | @ShapeShift.MessagePack.MsgPackDeformatter.ReadExtensionHeader(ShapeShift.Converters.Reader@)?displayProperty=nameWithType  |
| `MessagePackWriter.WriteNil`                   | @ShapeShift.Converters.Writer.WriteNull?displayProperty=nameWithType       |
| `MessagePackWriter.WriteExtensionFormat`       | @ShapeShift.MessagePack.MsgPackFormatter.Write(ShapeShift.Converters.Writer@,ShapeShift.MessagePack.Extension)?displayProperty=nameWithType       |
| `MessagePackWriter.WriteExtensionFormatHeader` | @ShapeShift.MessagePack.MsgPackFormatter.Write(ShapeShift.Converters.Writer@,ShapeShift.MessagePack.ExtensionHeader)?displayProperty=nameWithType |
| `IMessagePackSerializationCallbackReceiver`    | @ShapeShift.ISerializationCallbacks                                                          |
