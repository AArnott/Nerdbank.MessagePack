# Unity support

## Summary

Nerdbank.MessagePack is ready for [Unity](https://unity.com/), both for its mono and its IL2CPP backend.
The NativeAOT readiness of this library makes it an ideal library for IL2CPP in your game.

Unity, for its part, is _almost_ ready for Nerdbank.MessagePack.
With a couple steps, **you can make it work today with its mono backend**.
The IL2CPP backend fails to AOT compile the IL in our testing.

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

### Referencing source-generated APIs fail to resolve in Visual Studio

Use of [witness classes](type-shapes.md#witness-classes) is required in unity due to its lack of a .NET 8 runtime.
When compiled, a source generator adds a `ShapeProvider` property to these witness classes that you must provide to various APIs that require an @PolyType.ITypeShapeProvider.
Unfortunately, unity does not arrange for the execution of these source generators when you are editing in Visual Studio, so you are likely to see errors reported in the Error List, despite the fact that Unity will succeed at compiling your code.

### Use of generic attributes

The [UnityRoslynUpdater README](https://github.com/DaZombieKiller/UnityRoslynUpdater?tab=readme-ov-file#c-11) warns against using generic attributes or your game will crash.
This warning does not apply for uses of the @PolyType.GenerateShapeAttribute`1 attribute because we don't use reflection to discover these attributes.

### `PolyType` name collision

Installing the Nerdbank.MessagePack nuget package brings down dependencies, including a PolyType package.
Because unity itself declares an unrelated `enum PolyType` type, source code that unity injects into your project may fail to compile with an error like the following:

> Library\PackageCache\com.unity.2d.animation@494a3b4e73a9\Editor\SkinningModule\OutlineGenerator\OutlineGenerator.cs(66,36): error CS0234: The type or namespace name 'ptSubject' does not exist in the namespace 'PolyType' (are you missing an assembly reference?)

This is tracked in [unity bug IN-94432](https://unity3d.atlassian.net/servicedesk/customer/portal/2/IN-94432).

The only workaround known so far is to replace the occurrences of `PolyType` in the file referenced in the error with the fully-qualified `UnityEditor.U2D.Animation.ClipperLib.PolyType` string.
This is a hack though, since it requires changing a file in the unity package cache for your project.

### IL2CPP failures

Building your project in il2cpp mode may fail with an error such as the one below.

This is tracked in [unity bug IN-94443](https://unity3d.atlassian.net/servicedesk/customer/portal/2/IN-94443).

```text
C:\Program Files\Unity\Hub\Editor\6000.0.36f1\Editor\Data\il2cpp\build\deploy\il2cpp.exe @Library\Bee\artifacts\rsp\15974634398270999093.rsp
Error: IL2CPP error (no further information about what managed code was being converted is available)
System.AggregateException: One or more errors occurred. (Specified cast is not valid.)
 ---> System.InvalidCastException: Specified cast is not valid.
   at System.Runtime.TypeCast.CheckCastClass(MethodTable*, Object) + 0x31
   at Unity.IL2CPP.DataModel.BuildLogic.Populaters.DefinitionPopulater.PopulateCustomAttrProvider(CecilSourcedAssemblyData, ICustomAttributeProvider) + 0x1a2
   at Unity.IL2CPP.DataModel.BuildLogic.Populaters.DefinitionPopulater.PopulateTypeDef(TypeContext, UnderConstructionMember`2) + 0x94
   at Unity.IL2CPP.DataModel.BuildLogic.DataModelBuilder.<PopulateCecilSourcedDefinitions>b__14_1(UnderConstructionMember`2 typeDef) + 0x143
   at System.Threading.Tasks.Parallel.<>c__DisplayClass44_0`2.<PartitionerForEachWorker>b__1(IEnumerator& partitionState, Int32 timeout, Boolean& replicationDelegateYieldedBeforeCompletion) + 0x286
--- End of stack trace from previous location ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw() + 0x20
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(Exception) + 0x13
   at System.Threading.Tasks.Parallel.<>c__DisplayClass44_0`2.<PartitionerForEachWorker>b__1(IEnumerator& partitionState, Int32 timeout, Boolean& replicationDelegateYieldedBeforeCompletion) + 0x646
   at System.Threading.Tasks.TaskReplicator.Replica.Execute() + 0x3a
   --- End of inner exception stack trace ---
   at System.Threading.Tasks.TaskReplicator.Run[TState](TaskReplicator.ReplicatableUserAction`1, ParallelOptions, Boolean) + 0x15d
   at System.Threading.Tasks.Parallel.PartitionerForEachWorker[TSource, TLocal](Partitioner`1, ParallelOptions, Action`1, Action`2, Action`3, Func`4, Func`5, Func`1, Action`1) + 0x23f
--- End of stack trace from previous location ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw() + 0x20
   at System.Threading.Tasks.Parallel.ThrowSingleCancellationExceptionOrOtherException(ICollection, CancellationToken, Exception) + 0x31
   at System.Threading.Tasks.Parallel.PartitionerForEachWorker[TSource, TLocal](Partitioner`1, ParallelOptions, Action`1, Action`2, Action`3, Func`4, Func`5, Func`1, Action`1) + 0x3e0
   at System.Threading.Tasks.Parallel.ForEachWorker[TSource, TLocal](IEnumerable`1, ParallelOptions, Action`1, Action`2, Action`3, Func`4, Func`5, Func`1, Action`1) + 0x17b
   at System.Threading.Tasks.Parallel.ForEach[TSource](IEnumerable`1, Action`1) + 0x68
   at Unity.IL2CPP.DataModel.BuildLogic.Utils.ParallelHelpers.ForEach[TSource](IEnumerable`1, Action`1, Boolean) + 0xc4
   at Unity.IL2CPP.DataModel.BuildLogic.DataModelBuilder.PopulateCecilSourcedDefinitions(ReadOnlyCollection`1) + 0x2e6
   at Unity.IL2CPP.DataModel.BuildLogic.DataModelBuilder.Build() + 0x23c
   at Unity.IL2CPP.AssemblyConversion.Phases.InitializePhase.Run(AssemblyConversionContext) + 0x319
   at Unity.IL2CPP.AssemblyConversion.Classic.ClassicConverter.Run(AssemblyConversionContext) + 0x11
   at Unity.IL2CPP.AssemblyConversion.AssemblyConverter.ConvertAssemblies(AssemblyConversionContext, ConversionMode) + 0x24
   at Unity.IL2CPP.AssemblyConversion.AssemblyConverter.ConvertAssemblies(TinyProfiler2, AssemblyConversionInputData, AssemblyConversionParameters, AssemblyConversionInputDataForTopLevelAccess) + 0x17f

UnityEditor.EditorApplication:Internal_CallDelayFunctions ()
```
