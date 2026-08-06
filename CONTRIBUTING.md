# Contributing

This project has adopted the [Microsoft Open Source Code of
Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct
FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com)
with any additional questions or comments.

## Best practices

* Use Windows PowerShell or [PowerShell Core][pwsh] (including on Linux/OSX) to run .ps1 scripts.
  Some scripts set environment variables to help you, but they are only retained if you use PowerShell as your shell.

## Prerequisites

All dependencies can be installed by running the `init.ps1` script at the root of the repository
using Windows PowerShell or [PowerShell Core][pwsh] (on any OS).
Some dependencies installed by `init.ps1` may only be discoverable from the same command line environment the init script was run from due to environment variables, so be sure to launch Visual Studio or build the repo from that same environment.
Alternatively, run `init.ps1 -InstallLocality Machine` (which may require elevation) in order to install dependencies at machine-wide locations so Visual Studio and builds work everywhere.

The only prerequisite for building, testing, and deploying from this repository
is the [.NET SDK](https://get.dot.net/).
You should install the version specified in `global.json` or a later version within
the same major.minor.Bxx "hundreds" band.
For example if 2.2.300 is specified, you may install 2.2.300, 2.2.301, or 2.2.310
while the 2.2.400 version would not be considered compatible by .NET SDK.
See [.NET Core Versioning](https://learn.microsoft.com/dotnet/core/versions/) for more information.

## Package restore

The easiest way to restore packages may be to run `init.ps1` which automatically authenticates
to the feeds that packages for this repo come from, if any.
`dotnet restore` or `nuget restore` also work but may require extra steps to authenticate to any applicable feeds.

## Building

This repository can be built on Windows, Linux, and OSX.

Building, testing, and packing this repository can be done by using the standard dotnet CLI commands (e.g. `dotnet build`, `dotnet test`, `dotnet pack`, etc.).

[pwsh]: https://learn.microsoft.com/powershell/scripting/install/installing-powershell

## Testing

You can use `dotnet test` to build and/or test the repo.

There may be tests that are known to be unstable or have special requirements. These can be avoided by running tests using the [dotnet-test-cloud.ps1](tools/dotnet-test-cloud.ps1) script *after* running `dotnet build`.

## Releases

For stable releases, use `nbgv prepare-release` to create and push the `v*.*` release branch, then wait for its build to complete. Pre-release packages may be released directly from a successful `main` build.

Run the **Prepare release** workflow on the branch containing the commit to release. It uses `nbgv tag` to tag that commit and creates a draft GitHub Release. Review and publish the release. Publishing resubmits the build's test-signing request to the manually approved `release-signing` policy.

### GitHub Actions

When your repo is hosted by GitHub and you are using GitHub Actions, you should create a GitHub Release using the standard GitHub UI.
[Learn more about the `nbgv tag` and `prepare-release` commands](https://dotnet.github.io/Nerdbank.GitVersioning/docs/nbgv-cli.html).

After publishing the release, the `.github/workflows/release.yml` workflow will be automatically triggered, which will:

1. Find the most recent `.github/workflows/build.yml` GitHub workflow run of the tagged release.
1. Download the test-signing request metadata from that workflow run.
1. Resubmit the exact tested artifact to SignPath's manually approved `release-signing` policy.
1. Upload the release-signed packages to the GitHub Release and nuget.org.

### Code signing

NuGet packages are signed through [SignPath.io](https://docs.signpath.io/trusted-build-systems/github). Non-PR Linux builds submit the GitHub-hosted `deployables` artifact to the `test-signing` policy and retain the signing request ID. When a release is published, the release workflow accepts a successful build from `main` or a `v*.*` release branch and resubmits that exact signing request to the manually approved `release-signing` policy.

Configure these GitHub repository settings before merging signing workflow changes:

| Setting | Kind | Value |
| --- | --- | --- |
| `SIGNPATH_API_TOKEN` | Secret | Token for a SignPath CI user that can submit to both signing policies |
| `SIGNPATH_ORGANIZATION_ID` | Variable | SignPath organization GUID |
| `SIGNPATH_PROJECT_SLUG` | Variable | SignPath project slug |
| `SIGNPATH_ARTIFACT_CONFIGURATION_SLUG` | Variable | Artifact configuration slug for the NuGet package bundle |

The SignPath project must use `https://github.com/AArnott/Nerdbank.MessagePack` as its repository URL and GitHub Actions as a trusted build system. Configure `test-signing` without approval so CI can exercise signing immediately. Configure `release-signing` with origin verification restricted to `main` and `v*.*` branches and manual approval, as required by the [SignPath Foundation terms](https://signpath.org/terms).

The artifact configuration must use a `<zip-file>` root because GitHub's artifact action submits a ZIP. Upload a representative `deployables-Linux` artifact to SignPath to generate and review the configuration. It must sign every `*.nupkg` with `<nuget-sign>`, preserve the `*.snupkg` files, enforce the project name and the `version` parameter supplied by the workflow, and reject unexpected files.

### Azure Pipelines

When your repo builds with Azure Pipelines, use the `azure-pipelines/release.yml` pipeline.
Trigger the pipeline by adding the `auto-release` tag on a run of your main `azure-pipelines.yml` pipeline.

## Tutorial and API documentation

The site at https://aarnott.github.io/Nerdbank.MessagePack builds from this repo's `docfx/` directory.
The site is updated on every push to the `main` branch.

You can make changes and host the site locally to preview them by switching to that directory and running the `dotnet docfx --serve` command.
After making a change, you can rebuild the docs site while the localhost server is running by running `dotnet docfx` again from a separate terminal.

[Learn more about docfx](https://dotnet.github.io/docfx/).

## Updating dependencies

This repo uses Renovate to keep dependencies current.
Configuration is in the `.github/renovate.json` file.
[Learn more about configuring Renovate](https://docs.renovatebot.com/configuration-options/).

When changing the renovate.json file, follow [these validation steps](https://docs.renovatebot.com/config-validation/).

If Renovate is not creating pull requests when you expect it to, check that the [Renovate GitHub App](https://github.com/apps/renovate) is configured for your account or repo.

## Merging latest from Library.Template

### Maintaining your repo based on this template

The best way to keep your repo in sync with Library.Template's evolving features and best practices is to periodically merge the template into your repo:

```ps1
git fetch
git checkout origin/main
./tools/MergeFrom-Template.ps1
# resolve any conflicts, then commit the merge commit.
git push origin -u HEAD
```

## Code editing guidelines

### Monitoring and mitigating code gen size

Optimized NativeAOT targeting is a very important feature of this library.
Not just that it's possible, but that the emitted code size is reasonably small.
We use PR/CI checks in our GitHub workflow to guard the emitted code size so that it does not creep up without intentional review.

We use the `sizoscope` .NET CLI tool to understand what contributes to our NativeAOT output size so we can understand what we can do to reduce the emitted binary size.

There are two very important things you can do to keep emitted binary size down:

1. Avoid using generic types and methods with user-defined types type arguments, *especially* if they might be value types.
   This often manifests as avoiding Linq extension methods when any type argument is not known to be a ref type.
   Value type arguments force unique native code to be emitted, multiplying the size of the native code.
2. When defining generic types that will close over user-defined types, make them classes instead of structs.
   This will help users of these types to follow guideline number one in this list.
3. Avoid declaring `record` where `class` or `struct` would suffice. The extra `IEquatable<T>` implementation bloats the code size, especially on generic types.
