# NBMsgPack102: Remove use of MessagePackObjectAttribute

This diagnostic is emitted where a `[MessagePack.MessagePackObject]` attribute is used.
A code fix is offered to remove this attribute, as it is no longer needed.

It *may* need to be replaced with use of <xref:PolyType.GenerateShapeAttribute> if this type is used in the top-level call to @Nerdbank.MessagePack.MessagePackSerializer.Serialize* or @Nerdbank.MessagePack.MessagePackSerializer.Deserialize*.

[Learn more about migrating](../docs/migrating.md).
