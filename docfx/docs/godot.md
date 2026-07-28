# Godot support

The `Nerdbank.MessagePack.Godot` package adds compact MessagePack converters for common Godot Engine value types.
It supports Godot projects targeting .NET 8 or later and is compatible with Native AOT.

## Installation

Install the `Nerdbank.MessagePack.Godot` NuGet package.

## Configuration

Configure each <xref:Nerdbank.MessagePack.MessagePackSerializer> instance that serializes Godot values with <xref:Nerdbank.MessagePack.Godot.GodotMessagePackSerializerExtensions.WithGodotConverters*>:

```cs
using Godot;
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.Godot;
using PolyType;

[GenerateShapeFor<Vector3>]
internal partial class GodotShapes;

MessagePackSerializer serializer = new MessagePackSerializer().WithGodotConverters();
Vector3 position = new(1.5f, 2f, -4f);
byte[] bytes = serializer.Serialize<Vector3, GodotShapes>(position);
Vector3 roundTripped = serializer.Deserialize<Vector3, GodotShapes>(bytes);
```

When a Godot value is a member of your own serializable type, apply <xref:PolyType.GenerateShapeAttribute> to that type as usual.
The package's converters are then selected automatically for the Godot members.

## Supported types

The package supports these Godot value types:

* `Godot.Aabb`
* `Godot.Basis`
* `Godot.Color`
* `Godot.Plane`
* `Godot.Projection`
* `Godot.Quaternion`
* `Godot.Rect2` and `Godot.Rect2I`
* `Godot.Transform2D` and `Godot.Transform3D`
* `Godot.Vector2`, `Godot.Vector2I`, `Godot.Vector3`, `Godot.Vector3I`, `Godot.Vector4`, and `Godot.Vector4I`

Godot nodes, resources, variants, and collection types are not included. Serialize application-owned types that refer to them instead.

## MessagePack-CSharp migration

Each supported value is encoded as a positional MessagePack array in the same field order used by the `MessagePackGodot` package.
Existing payloads written by that package can therefore be read after switching to Nerdbank.MessagePack, and new payloads retain that wire format.
The converters ignore additional array elements when reading, allowing a future version to append values without breaking older readers.
