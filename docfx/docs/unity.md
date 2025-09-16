# Unity support

## Summary

Nerdbank.MessagePack is ready for [Unity](https://unity.com/), both for its mono and its IL2CPP backend.
The NativeAOT readiness of this library makes it an ideal library for IL2CPP in your game.

## Installation

1. Install [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) into your project.
1. Use the new "NuGet -> Manage NuGet Packages" command in the Unity Editor to install the `Nerdbank.MessagePack` package. You may need to set the option to show prerelease versions.

## Sample Unity game

A trivial Unity 'game' that demonstrates serializing and deserializing a custom type is available [here](https://github.com/AArnott/Nerdbank.MessagePack.UnitySandbox/commits/main/).

## Known issues

### .NET Standard APIs only

Unity is currently limited to .NET Framework or .NET Standard 2.1 libraries.
Some of the preferred APIs in Nerdbank.MessagePack are exposed uniquely to .NET 8+ projects.
As a result, when reviewing documentation and samples for this library, be sure to look at the samples in their ".NET Standard" form.

Be sure to have this at the top of each code file that uses the serializer so that extension methods are available to you, which fills in most of the gaps for non-.NET targeting projects:

```cs
using Nerdbank.MessagePack;
```

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

### The type or namespace name 'GenerateShapeAttribute' could not be found

In projects with disabled auto references, you will need to add an assembly reference to PolyType.dll and Nerdbank.MessagePack.dll to your project manually.

### Visual Studio complains that the Witness class has no GeneratedTypeShapeProvider property

The [witness class](type-shapes.md) that your unity project requires for serialization should be attributed with <xref:PolyType.GenerateShapeForAttribute> and be declared as a `partial class` (with at least `internal` visibility), after which the PolyType source generator should add a `GeneratedTypeShapeProvider` property.
Some configurations of Visual Studio will not re-run the source generator until you save your code file with these changes.
If you see an error (or a lack of `GeneratedTypeShapeProvider` in the completion list for your witness class), verify that you have the attribute and `partial` modifier as required, and save your code file.

Right-clicking the source file and asking Unity to "Reimport" that file is also reported to fixing this transient issue.

### MissingMethodException: Unsafe.IsNullRef

When running your Unity project, you might see the following error:

> MissingMethodException: Method not found: bool System.Runtime.CompilerServices.Unsafe.IsNullRef<!0>(!!0&)

This means your project is using an old version of `System.Runtime.CompilerServices.Unsafe.dll`.

To resolve the error, use NuGetForUnity to update this dependency to the latest.
