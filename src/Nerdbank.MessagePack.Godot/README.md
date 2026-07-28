# Nerdbank.MessagePack.Godot

MessagePack converters for common Godot Engine value types.

```cs
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.Godot;

MessagePackSerializer serializer = new MessagePackSerializer().WithGodotConverters();
```

See the [Godot documentation](https://aarnott.github.io/Nerdbank.MessagePack/docs/godot.html) for installation, supported types, Native AOT guidance, and MessagePack-CSharp migration compatibility.
