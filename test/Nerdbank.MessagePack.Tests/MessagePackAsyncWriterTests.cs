// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

public class MessagePackAsyncWriterTests
{
	[Fact]
	[Experimental("NBMsgPackAsync")]
	public void WriteVeryLargeData()
	{
		MessagePackAsyncWriter writer = new(PipeWriter.Create(Stream.Null));
		writer.WriteRaw(new byte[1024 * 1024]);
	}
}
