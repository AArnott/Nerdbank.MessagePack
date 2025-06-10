// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Converters;

public partial class MessagePackValueConverterTests : MessagePackSerializerTestBase
{
	[Fact]
	public void Null() => this.AssertRoundtrip(MessagePackValue.Nil);

	[Fact]
	public void Integer_Positive() => this.AssertRoundtrip<MessagePackValue>(5);

	[Fact]
	public void Integer_Negative() => this.AssertRoundtrip<MessagePackValue>(-5);

	[Fact]
	public void Boolean() => this.AssertRoundtrip<MessagePackValue>(true);

	[Fact]
	public void String() => this.AssertRoundtrip<MessagePackValue>("Hi");

	[Fact]
	public void Single() => this.AssertRoundtrip<MessagePackValue>(float.MaxValue);

	[Fact]
	public void Double() => this.AssertRoundtrip<MessagePackValue>(double.MaxValue);

	[Fact]
	public void Map() => this.AssertRoundtrip<MessagePackValue>(new Dictionary<MessagePackValue, MessagePackValue> { [1] = "hi" });

	[Fact]
	public void Array() => this.AssertRoundtrip<MessagePackValue>(new MessagePackValue[] { 5 });

	[Fact]
	public void Binary() => this.AssertRoundtrip<MessagePackValue>(new byte[] { 1, 2, 3 });

	[Fact]
	public void Extension() => this.AssertRoundtrip<MessagePackValue>(new Extension(-5, new byte[] { 1, 2 }));
}
