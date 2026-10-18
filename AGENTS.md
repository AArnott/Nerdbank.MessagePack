# Copilot instructions for this repository

## High level guidance

* Review the `CONTRIBUTING.md` file for instructions to build and test the software.
* Run the `.github/Prime-ForCopilot.ps1` script (once) before running any `dotnet` or `msbuild` commands.
  If you see any build errors about not finding git objects or a shallow clone, it may be time to run this script again.

## Performance optimization

* Establish a BenchmarkDotNet baseline before changing a hot path. For primitive integer encoding and decoding, run:
  ```bash
  dotnet run --project test/Benchmarks/Benchmarks.csproj -c Release -f net10.0 -- --filter "*IntegerPrimitives*" --job short
  ```
* Keep benchmark input distributions explicit and reproducible. `Small`, `Mixed`, and `Large` integer datasets exercise distinct MessagePack encodings and branch-prediction behavior; do not replace them with a single representative input.
* For branch-sensitive work, use sufficiently large randomized datasets so a branch predictor cannot learn a short repeating sequence. Preserve the fixed random seed unless intentionally changing the workload.
* Review allocation, generated assembly, branch instructions, and branch mispredictions alongside elapsed time. Hardware counters require an elevated Windows process; an unavailable counter is not evidence of zero misses.
* Benchmark changes measure behavior; they do not prove correctness. Verify all MessagePack encoding boundaries and error behavior with the relevant tests before accepting an optimization.
* Prefer narrowly targeted candidates and retain a simple, verified baseline until benchmark results and generated assembly demonstrate a repeatable improvement for the intended distributions.

## Repository Structure

### Key Projects (src/)
- `Nerdbank.MessagePack` - Main MessagePack serialization library
- `Nerdbank.MessagePack.SignalR` - SignalR integration
- `Nerdbank.MessagePack.AspNetCoreMvcFormatter` - ASP.NET Core MVC formatter
- `Nerdbank.MessagePack.Analyzers` - Roslyn analyzers and code fixes

### Test Projects (test/)
- Each shipping project has a corresponding `.Tests` project
- `AotNativeConsole` - NativeAOT compatibility validation
- `Benchmarks` - Performance benchmarks

### Samples (samples/)
- `AspNetMvc` - ASP.NET Core MVC integration example
- `SignalR` - SignalR integration example
- `cs` and `fs` - C# and F# usage examples

## Software Design

* Design APIs to be highly testable, and all functionality should be tested.
* Avoid introducing binary breaking changes in public APIs of projects under `src` unless their project files have `IsPackable` set to `false`.

## Coding style

* Honor StyleCop rules and fix any reported build warnings *after* getting tests to pass.
* In C# files, use namespace *statements* instead of namespace *blocks* for all new files.
* Add API doc comments to all new public and internal members in shipping code under `src`. Tests and samples do not require XML API documentation; do not add XML docs to test or sample members solely to satisfy this rule.

## Documentation

* When writing DocFX documentation (`.md` files in the `docfx` directory), use `<xref:symbolname>` syntax for cross-references to types and members instead of `@symbolname` syntax.
  * Example: Use `<xref:Nerdbank.MessagePack.ConverterContext>` instead of `@Nerdbank.MessagePack.ConverterContext`
  * This ensures proper linking and IDE support in the generated documentation.
* Code samples should be placed in the `samples/cs` directory with region tags and referenced from documentation using `[!code-csharp[](path#region)]` syntax.
  * This keeps samples compilable and tested as part of the build.

## Common Tasks

### After Making Changes
1. **Build**: `dotnet build tools/dirs.proj -t:build,pack --no-restore -c Release` (NEVER CANCEL - 7-76s)
2. **Test**: `dotnet test --no-build -c Release -- --treenode-filter "/*/*/*/*[TestCategory!=FailsInCloudTest]"` (25s)
3. **Format**: `dotnet format --verify-no-changes --no-restore` (NEVER CANCEL - 71s)
4. **Validate**: Run AOT console sample for functionality verification

### Documentation Updates
```bash
cd docfx
dotnet docfx --serve
# Make changes, then rebuild with:
dotnet docfx
```

### Troubleshooting
- **Build fails**: Ensure `NBGV_GitEngine=Disabled` is set
- **Long restore times**: Use `./init.ps1` to bootstrap dependencies first
- **Test instability**: Always use `-- --treenode-filter "/*/*/*/*[TestCategory!=FailsInCloudTest]"`
- **Format failures**: Run `dotnet format` (without `--verify-no-changes`) to fix automatically

## CRITICAL Timing Expectations
- **Dependency setup**: 2-3 seconds
- **Full build**: 7-76 seconds (fast with cache, slower on first build) (NEVER CANCEL - use 120+ minute timeouts)
- **Test suite**: ~25 seconds (NEVER CANCEL - use 60+ minute timeouts)
- **Format verification**: ~71 seconds (NEVER CANCEL - use 90+ minute timeouts)
- **Documentation build**: ~19 seconds

**NEVER CANCEL long-running commands** - these timing expectations are normal for this repository.
