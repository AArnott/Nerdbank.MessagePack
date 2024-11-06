; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NBMsgPack001 | Usage | Error | Apply `[Key]` consistently across members
NBMsgPack002 | Usage | Warning | Avoid `[Key]` on non-serialized members
NBMsgPack003 | Usage | Error | `[Key]` index must be unique
