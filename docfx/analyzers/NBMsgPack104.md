# NBMsgPack104: Remove use of IgnoreMemberAttribute

This diagnostic is emitted where a `[MessagePack.IgnoreMemberAttribute]` attribute is used.
A code fix is offered to remove this attribute or switch to with @PolyType.PropertyShapeAttribute with its @PolyType.PropertyShapeAttribute.Ignore property set to `true`.

[Learn more about migrating](../docs/migrating.md).
