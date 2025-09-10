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

## Releases

Use `nbgv tag` to create a tag for a particular commit that you mean to release.
[Learn more about `nbgv` and its `tag` and `prepare-release` commands](https://dotnet.github.io/Nerdbank.GitVersioning/docs/nbgv-cli.html).

Push the tag.

### GitHub Actions

When your repo is hosted by GitHub and you are using GitHub Actions, you should create a GitHub Release using the standard GitHub UI.
Having previously used `nbgv tag` and pushing the tag will help you identify the precise commit and name to use for this release.

After publishing the release, the `.github/workflows/release.yml` workflow will be automatically triggered, which will:

1. Find the most recent `.github/workflows/build.yml` GitHub workflow run of the tagged release.
1. Upload the `deployables` artifact from that workflow run to your GitHub Release.
1. If you have `NUGET_API_KEY` defined as a secret variable for your repo or org, any nuget packages in the `deployables` artifact will be pushed to nuget.org.

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
