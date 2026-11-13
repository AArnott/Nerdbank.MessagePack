# NBMsgPack105: Implement IMessagePackSerializationCallbacks

This diagnostic is emitted where `MessagePack.IMessagePackSerializationCallbackReceiver` is implemented.
A code fix is offered to replace this implementation with one of @Nerdbank.MessagePack.IMessagePackSerializationCallbacks.
The replacement interface is only supported on reference types.
A struct that implements it is reported by [NBMsgPack120](NBMsgPack120.md).

[Learn more about migrating](../docs/migrating.md).
