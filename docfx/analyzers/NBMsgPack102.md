# NBMsgPack102: Remove use of MessagePackObjectAttribute

This diagnostic is emitted where a `[MessagePack.MessagePackObject]` attribute is used.
A code fix is offered to remove this attribute, as it is no longer needed.

It _may_ need to be replaced with use of @PolyType.GenerateShapeAttribute if this type is used in the top-level call to @ShapeShift.MessagePackSerializer.Serialize* or @ShapeShift.MessagePackSerializer.Deserialize*.

[Learn more about migrating](../docs/migrating.md).
