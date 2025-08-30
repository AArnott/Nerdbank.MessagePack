# Copilot instructions for this repository

**ALWAYS follow these instructions first and only fallback to additional search and context gathering if the information here is incomplete or found to be in error.**

## Working Effectively

### Bootstrap and Build
**CRITICAL**: Set the `NBGV_GitEngine` environment variable to `Disabled` before running ANY `dotnet` or `msbuild` commands.

```bash
export NBGV_GitEngine=Disabled
```

**Setup dependencies** (takes ~2-3 seconds):
```bash
./init.ps1 -UpgradePrerequisites -NoNuGetCredProvider
```

**Build the repository** (takes 7-76 seconds depending on cache - NEVER CANCEL, set timeout to 120+ minutes):
```bash
dotnet build tools/dirs.proj -t:build,pack --no-restore -c Release
```

**NEVER CANCEL**: Build takes 7-76 seconds (fast with cache, slower on first build). Always use timeouts of 120+ minutes for build commands to avoid premature cancellation.

### Testing
**Run tests** (takes ~25 seconds - NEVER CANCEL, set timeout to 5-10 minutes):
```bash
dotnet test --no-build -c Release --filter "TestCategory!=FailsInCloudTest"

### Code Quality
**Verify code formatting** (takes ~71 seconds - NEVER CANCEL, set timeout to 90+ minutes):
```bash
dotnet format --verify-no-changes --no-restore
```

**Build documentation** (takes ~19 seconds):
```bash
dotnet docfx docfx/docfx.json --warningsAsErrors --disableGitFeatures
```

**NEVER CANCEL**: Code formatting verification takes approximately 71 seconds. This is normal and expected.

## Validation Scenarios
**ALWAYS test functionality after making changes by running validation scenarios:**

**Test AOT Native Console sample**:
```bash
cd test/AotNativeConsole
dotnet run --no-build -c Release
```
Expected output: JSON data followed by tree structure with fruits and seeds, ending with "Success".

**Test ASP.NET MVC integration**:
```bash
cd samples/AspNetMvc
dotnet run --no-build -c Release
```
Should start web server without errors (web UI testing limited in this environment).

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

## Testing

* There should generally be one test project (under the `test` directory) per shipping project (under the `src` directory). Test projects are named after the project being tested with a `.Tests` suffix.
* Tests should use the Xunit testing framework.
* Some tests are known to be unstable. When running tests, you should skip the unstable ones by running `dotnet test --filter "TestCategory!=FailsInCloudTest"`.
* Test suite contains 7767 total tests with ~7359 passing and ~408 skipped when using the stability filter.

## Coding Style

* Honor StyleCop rules and fix any reported build warnings *after* getting tests to pass.
* In C# files, use namespace *statements* instead of namespace *blocks* for all new files.
* Add API doc comments to all new public and internal members.
* Always run `dotnet format --verify-no-changes --no-restore` before committing changes or CI will fail.

## Common Tasks

### After Making Changes
1. **Build**: `dotnet build tools/dirs.proj -t:build,pack --no-restore -c Release` (NEVER CANCEL - 7-76s)
2. **Test**: `dotnet test --no-build -c Release --filter "TestCategory!=FailsInCloudTest"` (25s)
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
- **Test instability**: Always use the `TestCategory!=FailsInCloudTest` filter
- **Format failures**: Run `dotnet format` (without `--verify-no-changes`) to fix automatically

## CRITICAL Timing Expectations
- **Dependency setup**: 2-3 seconds
- **Full build**: 7-76 seconds (fast with cache, slower on first build) (NEVER CANCEL - use 120+ minute timeouts)
- **Test suite**: ~25 seconds (NEVER CANCEL - use 60+ minute timeouts)  
- **Format verification**: ~71 seconds (NEVER CANCEL - use 90+ minute timeouts)
- **Documentation build**: ~19 seconds

**NEVER CANCEL long-running commands** - these timing expectations are normal for this repository.
