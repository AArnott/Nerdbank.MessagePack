# Migrating from MessagePack-CSharp

To migrate from MessagePack-CSharp to Nerdbank.MessagePack, begin by adding a package reference to Nerdbank.MessagePack as described in the [Getting Started](getting-started.md) guide.

With the new package referenced, automated code fixes are immediately provided to aid in the mechanics of migration.
You should complete migration before removing references to the old `MessagePack` package so the analyzers and code fixes can do their best work.

The migration analyzers produce diagnostics that are not errors or warnings, so you may need to navigate to an actual piece of code using or implementing a type from MessagePack-CSharp and activate the code fixes menu (<kbd>Ctrl</kbd>+<kbd>.</kbd> or Quick Actions in Visual Studio) to see the migration options.
When you activate the migration code fix, you will have the option to apply the code fix to all occurrences in the solution rather than just the one you found, which can speed up your migration process.

Sometimes after applying one migration code fix, a subsequent analyzer will report new diagnostics, guiding you to the next step in migration.

The following sections demonstrate the changes that are required to migrate from MessagePack-CSharp to Nerdbank.MessagePack.
Remember that automated code fixes can do most or all of this for you.

## `MessagePackObjectAttribute`

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

Nerdbank.MessagePack also supports @Nerdbank.MessagePack.KeyAttribute, which serves the same function as in MessagePack-CSharp: to change the serialized schema from that of a map of property name=value to an array of values.
Thus, you may keep the `[Key(0)]`, `[Key(1)]`, etc., attributes on your types if you wish to maintain the schema of the serialized data.

## `UnionAttribute`

MessagePack-CSharp defines a `UnionAttribute` by which you can serialize an object when you know its base type or interface at compile-time, but whose exact type is not known until runtime, provided you can predict the closed set of allowed runtime types in advance.
Nerdbank.MessagePack supports this same use case via its @Nerdbank.MessagePack.KnownSubTypeAttribute, and migration is straightforward:

```diff
-[Union(0, typeof(MyType1))]
-[Union(1, typeof(MyType2))]
+[KnownSubType(0, typeof(MyType1))]
+[KnownSubType(1, typeof(MyType2))]
 public interface IMyType
 {
 }
```

Any types referenced by the @Nerdbank.MessagePack.KnownSubTypeAttribute must be annotated with @PolyType.GenerateShapeAttribute as described above.

## `IMessagePackFormatter<T>`

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

## `MessagePackFormatterAttribute`

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

## Security mitigations

In MessagePack-CSharp, security mitigations are provided by the `MessagePackSecurity` class, as referenced by the `MessagePackSerializerOptions` class.

In Nerdbank.MessagePack, security mitigations are provided by the @Nerdbank.MessagePack.SerializationContext struct, as referenced by @Nerdbank.MessagePack.MessagePackSerializer.StartingContext?displayProperty=nameWithType.

## Incompatibilities

Some functionality in MessagePack-CSharp has no equivalent in Nerdbank.MessagePack, as follows:

- Typeless serialization: MessagePack-CSharp supports serializing and deserializing objects without knowing their type at compile time.
  Nerdbank.MessagePack requires knowing at least something about the type (see [Unions](unions.md)) at compile time for security reasons and NativeAOT support.
  [Custom converters](custom-converters.md) can be written to overcome these limitations where required.

## Other API changes

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
