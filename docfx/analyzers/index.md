# MessagePack analyzers

The `Nerdbank.MessagePack` nuget packages comes with C# analyzers to help you author valid code.
They will emit diagnostics with warnings or errors depending on the severity of the issue.

Some of these diagnostics will include a suggested code fix that can apply the correction to your code automatically.

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
[NBMsgPack001](NBMsgPack001.md) | Usage | Error | Apply `[Key]` consistently across members
[NBMsgPack002](NBMsgPack002.md) | Usage | Warning | Avoid `[Key]` on non-serialized members
[NBMsgPack003](NBMsgPack003.md) | Usage | Error | `[Key]` index must be unique
[NBMsgPack010](NBMsgPack010.md) | Usage | Error | `[DerivedTypeShape]` should specify an assignable type
[NBMsgPack011](NBMsgPack011.md) | Usage | Error | `[DerivedTypeShape]` alias must be unique
[NBMsgPack012](NBMsgPack012.md) | Usage | Error | `[DerivedTypeShape]` type must be unique
[NBMsgPack020](NBMsgPack020.md) | Usage | Error | `[MessagePackConverter]` type must be compatible converter
[NBMsgPack021](NBMsgPack021.md) | Usage | Error | `[MessagePackConverter]` type missing default constructor
[NBMsgPack030](NBMsgPack030.md) | Usage | Warning | Converters should not call top-level `MessagePackSerializer` methods
[NBMsgPack031](NBMsgPack031.md) | Usage | Warning | Converters should read or write exactly one msgpack structure
[NBMsgPack032](NBMsgPack032.md) | Usage | Info | Converters should override @Nerdbank.MessagePack.MessagePackConverter`1.GetJsonSchema*
[NBMsgPack033](NBMsgPack033.md) | Usage | Error | Async converters should return writers
[NBMsgPack034](NBMsgPack034.md) | Usage | Error | Async converters should not reuse MessagePackWriter after returning it
[NBMsgPack035](NBMsgPack035.md) | Usage | Error | Async converters should return readers
[NBMsgPack036](NBMsgPack036.md) | Usage | Error | Async converters should not reuse readers after returning them
[NBMsgPack037](NBMsgPack037.md) | Usage | Warning | Async converters should override @Nerdbank.MessagePack.MessagePackConverter`1.PreferAsyncSerialization
[NBMsgPack050](NBMsgPack050.md) | Usage | Warning | Use ref parameters for ref structs
[NBMsgPack051](NBMsgPack051.md) | Usage | Warning | Prefer modern .NET APIs
[NBMsgPack060](NBMsgPack060.md) | Usage | Error | @Nerdbank.MessagePack.UnusedDataPacket member should have a property shape
[NBMsgPack061](NBMsgPack061.md) | Usage | Error | @Nerdbank.MessagePack.UnusedDataPacket member should not have a KeyAttribute
[NBMsgPack062](NBMsgPack062.md) | Usage | Warning | @Nerdbank.MessagePack.UnusedDataPacket properties should be private
[NBMsgPack070](NBMsgPack070.md) | Usage | Error | UseComparerAttribute type must not be an open generic
[NBMsgPack071](NBMsgPack071.md) | Usage | Error | UseComparerAttribute member name must point to a valid property
[NBMsgPack072](NBMsgPack072.md) | Usage | Error | UseComparerAttribute must specify a compatible comparer
[NBMsgPack073](NBMsgPack073.md) | Usage | Error | UseComparerAttribute type must not be abstract unless using static member
[NBMsgPack100](NBMsgPack100.md) | Migration | Info | Migrate MessagePack-CSharp formatter
[NBMsgPack101](NBMsgPack101.md) | Migration | Info | Migrate to @Nerdbank.MessagePack.MessagePackConverterAttribute
[NBMsgPack102](NBMsgPack102.md) | Migration | Info | Remove use of MessagePackObjectAttribute
[NBMsgPack103](NBMsgPack103.md) | Migration | Info | Use newer @Nerdbank.MessagePack.KeyAttribute
[NBMsgPack104](NBMsgPack104.md) | Migration | Info | Remove use of IgnoreMemberAttribute
[NBMsgPack105](NBMsgPack105.md) | Migration | Info | Implement @Nerdbank.MessagePack.IMessagePackSerializationCallbacks
[NBMsgPack106](NBMsgPack106.md) | Migration | Info | Use <xref:PolyType.ConstructorShapeAttribute>
