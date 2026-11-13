# NBMsgPack120: IMessagePackSerializationCallbacks is only supported on reference types

This diagnostic is reported when a struct implements <xref:Nerdbank.MessagePack.IMessagePackSerializationCallbacks>.
The interface is only supported on reference types.
Object map and array converters invoke `OnBeforeSerialize`, `OnAfterSerialize`, `OnBeforeDeserialize`, and `OnAfterDeserialize` by casting the value to the interface.
When the value is a struct, that cast boxes a copy, so mutations made in the callback methods are lost.

The diagnostic includes every struct form: an ordinary struct, a `readonly struct`, a `ref struct`, and a `record struct`.
Implementing the interface through another interface is reported as well.

## Example violation

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack120.cs#Defective)]

## Resolution

Implement the interface on a class or record class:

[!code-csharp[](../../samples/cs/AnalyzerDocs/NBMsgPack120.cs#Fix)]
