# Nerdbank.MessagePack.Godot

MessagePack converters for common Godot Engine value types.

```cs
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.Godot;

MessagePackSerializer serializer = new MessagePackSerializer().WithGodotConverters();
```

See the repository's Godot documentation for installation, supported types, Native AOT guidance, and MessagePack-CSharp migration compatibility.
