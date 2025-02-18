# NBMsgPack100: Migrate MessagePack-CSharp formatter

This diagnostic is emitted where an implementation of `MessagePack.IMessagePackFormatter<T>` is found.
A code fix is offered to upgrade the formatter to one that derives from @ShapeShift.MessagePackConverter`1.

[Learn more about migrating](../docs/migrating.md).
