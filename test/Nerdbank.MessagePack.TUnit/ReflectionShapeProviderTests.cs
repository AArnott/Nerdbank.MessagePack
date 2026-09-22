// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

public class ReflectionShapeProviderTests : MessagePackSerializerTestBase
{
	[Test]
#if NET
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void SerializeUnshapedType()
	{
#if NET
		if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled)
		{
			Skip.Test("PolyType's reflection-based shape provider requires runtime code generation, which is not available under NativeAOT.");
		}
#endif

		Person person = new("Andrew", "Arnott");
		ITypeShape<Person> shape = PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default.GetTypeShape<Person>();
		byte[] msgpack = this.Serializer.Serialize(person, shape, this.TimeoutToken);
		this.LogMsgPack(msgpack);
		Person? deserialized = this.Serializer.Deserialize(msgpack, shape, this.TimeoutToken);
		Assert.Equal(person, deserialized);
	}

	public record Person(string FirstName, string LastName);
}
