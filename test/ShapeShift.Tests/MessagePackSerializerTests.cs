// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Sdk;

public partial class MessagePackSerializerTests() : SharedSerializerTests<MessagePackSerializer>(new MessagePackSerializer
{
	// Most async tests primarily mean to exercise the async code paths,
	// so disable the buffer that would lead it down the synchronous paths since we have
	// small test data sizes.
	MaxAsyncBuffer = 0,
})
{
}
