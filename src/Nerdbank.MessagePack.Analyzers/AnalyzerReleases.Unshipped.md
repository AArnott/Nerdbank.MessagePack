; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NBMsgPack001 | Usage | Error | Apply `[Key]` consistently across members
NBMsgPack002 | Usage | Warning | Avoid `[Key]` on non-serialized members
NBMsgPack003 | Usage | Error | `[Key]` index must be unique
NBMsgPack010 | Usage | Error | `[KnownSubType]` should specify an assignable type
NBMsgPack011 | Usage | Error | `[KnownSubType]` alias must be unique
NBMsgPack012 | Usage | Error | `[KnownSubType]` type must be unique
NBMsgPack013 | Usage | Error | `[KnownSubType]` type must not be an open generic
NBMsgPack020 | Usage | Error | `[MessagePackConverter]` type must be compatible converter
NBMsgPack021 | Usage | Error | `[MessagePackConverter]` type missing default constructor
NBMsgPack030 | Usage | Warning | Converters should not call top-level `MessagePackSerializer` methods
NBMsgPack031 | Usage | Warning | Converters should read or write exactly one msgpack structure
