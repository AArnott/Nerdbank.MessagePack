﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK

using System.Diagnostics;
using System.Runtime.CompilerServices;
using PolyType.ReflectionProvider;

public partial class AssemblyLoadTests
{
	[Fact(Skip = "Known to fail due to https://github.com/eiriktsarpalis/PolyType/issues/252")]
	public void SystemNumericsNotLoaded()
	{
		Helper(driver => driver.SerializeSomethingSimple(), "System.Numerics");
	}

	[Fact]
	public void PrimitiveConverterDoesNotLoadOtherAssemblies()
	{
		Helper(driver => driver.SerializeRoundtripWithReflectionProvider(true), "System.Numerics", "System.Drawing");
	}

	private static void Helper(Action<AppDomainTestDriver> action, params string[] disallowedAssemblies)
	{
		AppDomain testDomain = CreateTestAppDomain();
		try
		{
			var driver = (AppDomainTestDriver)testDomain.CreateInstanceAndUnwrap(typeof(AppDomainTestDriver).Assembly.FullName, typeof(AppDomainTestDriver).FullName);

			PrintLoadedAssemblies(driver);

			action(driver);

			PrintLoadedAssemblies(driver);
			driver.ThrowIfAssembliesLoaded(disallowedAssemblies);
		}
		finally
		{
			AppDomain.Unload(testDomain);
		}
	}

	private static AppDomain CreateTestAppDomain([CallerMemberName] string testMethodName = "") => AppDomain.CreateDomain($"Test: {testMethodName}", null, AppDomain.CurrentDomain.SetupInformation);

	private static IEnumerable<string> PrintLoadedAssemblies(AppDomainTestDriver driver)
	{
		var assembliesLoaded = driver.GetLoadedAssemblyList();
		TestContext.Current.TestOutputHelper?.WriteLine($"Loaded assemblies: {Environment.NewLine}{string.Join(Environment.NewLine, assembliesLoaded.OrderBy(s => s).Select(s => "   " + s))}");
		return assembliesLoaded;
	}

	[GenerateShape]
	internal partial record SomeObject(int Value);

#pragma warning disable CA1812 // Avoid uninstantiated internal classes
	private partial class AppDomainTestDriver : MarshalByRefObject
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
	{
#pragma warning disable CA1822 // Mark members as static -- all members must be instance for marshalability

		private readonly Dictionary<string, StackTrace> loadingStacks = new Dictionary<string, StackTrace>(StringComparer.OrdinalIgnoreCase);

		public AppDomainTestDriver()
		{
			AppDomain.CurrentDomain.AssemblyLoad += (s, e) =>
			{
				string simpleName = e.LoadedAssembly.GetName().Name;
				if (!this.loadingStacks.ContainsKey(simpleName))
				{
					this.loadingStacks.Add(simpleName, new StackTrace(skipFrames: 2, fNeedFileInfo: true));
				}
			};
		}

		internal string[] GetLoadedAssemblyList() => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).ToArray();

		internal void ThrowIfAssembliesLoaded(params string[] assemblyNames)
		{
			foreach (string assemblyName in assemblyNames)
			{
				if (this.loadingStacks.TryGetValue(assemblyName, out StackTrace? loadingStack))
				{
					throw new Exception($"Assembly {assemblyName} was loaded unexpectedly by the test with this stack trace: {Environment.NewLine}{loadingStack}");
				}
			}
		}

		internal void SerializeSomethingSimple()
		{
			MessagePackSerializer serializer = new();
			serializer.Serialize(new SomeObject(42));
		}

		internal T SerializeRoundtripWithReflectionProvider<T>(T value)
		{
			MessagePackSerializer serializer = new();

			// Use ReflectionTypeShapeProvider to avoid https://github.com/eiriktsarpalis/PolyType/issues/252
			ITypeShapeProvider typeProvider = ReflectionTypeShapeProvider.Default;
			byte[] buffer = serializer.Serialize(value, typeProvider);
			return serializer.Deserialize<T>(buffer, typeProvider)!;
		}

#pragma warning restore CA1822 // Mark members as static

		[GenerateShapeFor<bool>]
		private partial class Witness;
	}
}

#endif
