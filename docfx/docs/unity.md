# Unity support

## Summary

Nerdbank.MessagePack is ready for [Unity](https://unity.com/), both for its mono and its IL2CPP backend.
The NativeAOT readiness of this library makes it an ideal library for IL2CPP in your game.

Unity, for its part, is _almost_ ready for Nerdbank.MessagePack.
With a couple steps, **you can make it work today**.

## Prerequisites

In addition to of course having Unity installed, you must upgrade the C# compiler it uses to a newer one that supports C# 12+.
C# 12 introduced support for generic attributes, which is a fundamental requirement PolyType, upon which Nerdbank.MessagePack is based.

Use the [UnityRoslynUpdater](https://github.com/DaZombieKiller/UnityRoslynUpdater) to upgrade the C# compiler.

## In your unity project

1. Follow the [instructions on UnityRoslynUpdater](https://github.com/DaZombieKiller/UnityRoslynUpdater?tab=readme-ov-file#using-c-12) to enable C# 12+ language versions for your project. Nerdbank.MessagePack requires at least C# 12, though in general using the latest stable version is preferable.

1. Install [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) into your project.
1. Use the new "NuGet -> Manage NuGet Packages" command in the Unity Editor to install the `Nerdbank.MessagePack` package. You may need to set the option to show prerelease versions.

## Known issues

### .NET Standard APIs only

Unity is currently limited to .NET Framework or .NET Standard 2.1 libraries.
Some of the preferred APIs in Nerdbank.MessagePack are exposed uniquely to .NET 8+ projects.
As a result, when reviewing documentation and samples for this library, be sure to look at the samples in their ".NET Standard" form.

### Use of generic attributes

The [UnityRoslynUpdater README](https://github.com/DaZombieKiller/UnityRoslynUpdater?tab=readme-ov-file#c-11) warns against using generic attributes or your game will crash.
This warning does not apply for uses of the @PolyType.GenerateShapeAttribute`1 attribute because the attribute is never included in your compiled assembly.

### PolyType name collisions

If you encounter a compilation error such as the following:

> error CS0234: The type or namespace name 'ptSubject' does not exist in the namespace 'PolyType'"

Resolve it by taking the following steps:

1. Turn off "Auto Reference" for the conflicting assemblies.
   1. Within the Unity Editor, navigate to the Assets\Packages\PolyType.*\lib\libstandard2.x folder in your Project.
   1. Select the PolyType.dll file.
   1. Within the Inspector, uncheck the "Auto Reference" option. Click Apply.
   ![](../images/TurnOffAutoReference.png)
   1. Repeat these steps to turn off Auto Reference from Assets\Packages\Nerdbank.MessagePack.*\lib\netstandard2.x as well.
1. Manually reference these two assemblies from your code.
   1. Within the Unity editor, create an Assembly Definition or select your existing one.
   1. In the Inspector, check the "Override References" option.
   1. Under Assembly References, click the Add button. Select the PolyType.dll assembly.
   1. Repeat the prior step, this time selecting to add a reference to the Nerdbank.MessagePack.dll.
   ![](../images/ReferencePolyTypeInAssemblyDefinition.png)
   1. Scroll down as necessary in the Inspector and click Apply.
