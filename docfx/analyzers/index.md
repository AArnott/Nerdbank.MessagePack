# MessagePack analyzers

The `Nerdbank.MessagePack` nuget packages comes with C# analyzers to help you author valid code.
They will emit diagnostics with warnings or errors depending on the severity of the issue.

Some of these diagnostics will include a suggested code fix that can apply the correction to your code automatically.

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
[NBMsgPack001](NBMsgPack001.md) | Usage | Error | Apply `[Key]` consistently across members
[NBMsgPack002](NBMsgPack002.md) | Usage | Warning | Avoid `[Key]` on non-serialized members
[NBMsgPack003](NBMsgPack003.md) | Usage | Error | `[Key]` index must be unique
[NBMsgPack010](NBMsgPack010.md) | Usage | Error | `[KnownSubType]` should specify an assignable type
[NBMsgPack011](NBMsgPack011.md) | Usage | Error | `[KnownSubType]` alias must be unique
[NBMsgPack012](NBMsgPack012.md) | Usage | Error | `[KnownSubType]` type must be unique
[NBMsgPack020](NBMsgPack020.md) | Usage | Error | `[MessagePackConverter]` type must be compatible converter
[NBMsgPack021](NBMsgPack021.md) | Usage | Error | `[MessagePackConverter]` type missing default constructor
[NBMsgPack030](NBMsgPack030.md) | Usage | Warning | Converters should not call top-level `MessagePackSerializer` methods
[NBMsgPack031](NBMsgPack031.md) | Usage | Warning | Converters should read or write exactly one msgpack structure
[NBMsgPack100](NBMsgPack100.md) | Migration | Info | Migrate MessagePack-CSharp formatter
[NBMsgPack101](NBMsgPack101.md) | Migration | Info | Migrate to MessagePackConverterAttribute
[NBMsgPack102](NBMsgPack102.md) | Migration | Info | Remove use of MessagePackObjectAttribute
[NBMsgPack103](NBMsgPack103.md) | Migration | Info | Use newer KeyAttribute
[NBMsgPack104](NBMsgPack104.md) | Migration | Info | Remove use of IgnoreMemberAttribute
