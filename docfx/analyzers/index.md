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
[NBMsgPack012](NBMsgPack013.md) | Usage | Error | `[KnownSubType]` type must not be an open generic
