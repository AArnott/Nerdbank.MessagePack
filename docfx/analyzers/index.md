# MessagePack analyzers

The `Nerdbank.MessagePack` nuget packages comes with C# analyzers to help you author valid code.
They will emit diagnostics with warnings or errors depending on the severity of the issue.

Some of these diagnostics will include a suggested code fix that can apply the correction to your code automatically.

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
[NBMsgPack001](NBMsgPack001.md) | Usage | Error | Apply `[Key]` consistently across members
[NBMsgPack002](NBMsgPack002.md) | Usage | Warning | Avoid `[Key]` on non-serialized members
