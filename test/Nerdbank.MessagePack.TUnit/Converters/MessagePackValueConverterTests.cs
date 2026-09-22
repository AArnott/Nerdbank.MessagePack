// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Converters;

public partial class MessagePackValueConverterTests : MessagePackSerializerTestBase
{
	[Test]
	public void Null() => this.AssertRoundtrip(MessagePackValue.Nil);

	[Test]
	public void Integer_Positive() => this.AssertRoundtrip<MessagePackValue>(5);

	[Test]
	public void Integer_Negative() => this.AssertRoundtrip<MessagePackValue>(-5);

	[Test]
	public void Boolean() => this.AssertRoundtrip<MessagePackValue>(true);

	[Test]
	public void String() => this.AssertRoundtrip<MessagePackValue>("Hi");

	[Test]
	public void Single() => this.AssertRoundtrip<MessagePackValue>(float.MaxValue);

	[Test]
	public void Double() => this.AssertRoundtrip<MessagePackValue>(double.MaxValue);

	[Test]
	public void Map() => this.AssertRoundtrip<MessagePackValue>(new Dictionary<MessagePackValue, MessagePackValue> { [1] = "hi" });

	[Test]
	public void Array() => this.AssertRoundtrip<MessagePackValue>(new MessagePackValue[] { 5 });

	[Test]
	public void Binary() => this.AssertRoundtrip<MessagePackValue>(new byte[] { 1, 2, 3 });

	[Test]
	public void Extension() => this.AssertRoundtrip<MessagePackValue>(new Extension(-5, new byte[] { 1, 2 }));
}
