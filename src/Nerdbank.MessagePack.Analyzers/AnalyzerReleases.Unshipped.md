; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NBMsgPack001 | Usage | Error | Apply `[Key]` consistently across members
NBMsgPack002 | Usage | Warning | Avoid `[Key]` on non-serialized members
NBMsgPack003 | Usage | Error | `[Key]` index must be unique
NBMsgPack020 | Usage | Error | `[MessagePackConverter]` type must be compatible converter
NBMsgPack021 | Usage | Error | `[MessagePackConverter]` type missing default constructor
NBMsgPack030 | Usage | Warning | Converters should not call top-level `MessagePackSerializer` methods
NBMsgPack031 | Usage | Warning | Converters should read or write exactly one msgpack structure
NBMsgPack032 | Usage | Info | Converters should override GetJsonSchema
NBMsgPack033 | Usage | Error | Async converters should return the MessagePackWriter
NBMsgPack034 | Usage | Error | Async converters should not reuse MessagePackWriter after returning it
NBMsgPack035 | Usage | Error | Async converters should return readers
NBMsgPack036 | Usage | Error | Async converters should not reuse readers after returning them
NBMsgPack037 | Usage | Warning | Async converters should override PreferAsyncSerialization
NBMsgPack050 | Usage | Warning | Use ref parameters for ref structs
NBMsgPack051 | Usage | Warning | Prefer modern .NET APIs
NBMsgPack100 | Migration | Info | Migrate MessagePack-CSharp formatter
NBMsgPack101 | Migration | Info | Migrate to MessagePackConverterAttribute
NBMsgPack102 | Migration | Info | Remove use of MessagePackObjectAttribute
NBMsgPack103 | Migration | Info | Use newer KeyAttribute
NBMsgPack104 | Migration | Info | Remove use of IgnoreMemberAttribute
NBMsgPack105 | Migration | Info | Implement IMessagePackSerializationCallbacks
NBMsgPack106 | Migration | Info | Use ConstructorShapeAttribute
