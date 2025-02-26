// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public abstract class JsonSerializerTestBase : SerializerTestBase<JsonSerializer>
{
	public JsonSerializerTestBase()
		: base(new JsonSerializer())
	{
	}
}
