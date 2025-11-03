// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////#define COLDSTARTTEST

#if COLDSTARTTEST

TimeSpan first = OneOff(Visit_LargeDataModel);
TimeSpan second = OneOff(Visit_LargeDataModel);
Console.WriteLine($"First: {first}");
Console.WriteLine($"Second: {second}");

static void Visit_LargeDataModel()
{
	MessagePackSerializer serializer = new();
	_ = serializer.Serialize(null, PolyType.SourceGenerator.TypeShapeProvider_AotNativeConsole.Default.LargeDataModel);
}

static TimeSpan OneOff(Action test)
{
	Stopwatch timer = Stopwatch.StartNew();
	test();
	timer.Stop();
	return timer.Elapsed;
}

#else

await StreamingTree.RunAsync();
ManyModels.Run();

#endif
