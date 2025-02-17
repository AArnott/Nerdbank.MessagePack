// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

public class MessagePackAsyncWriterTests
{
	[Fact]
	[Experimental("NBMsgPackAsync")]
	public void WriteVeryLargeData()
	{
		AsyncWriter writer = new(PipeWriter.Create(Stream.Null), MsgPackFormatter.Default);
		writer.WriteRaw(new byte[1024 * 1024]);
	}
}
