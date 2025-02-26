// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public abstract class SerializerTestBase<TSerializer>(TSerializer defaultSerializer) : SerializerTestBase(defaultSerializer)
	where TSerializer : SerializerBase
{
	protected new TSerializer Serializer
	{
		get => (TSerializer)base.Serializer;
		set => base.Serializer = value;
	}
}
