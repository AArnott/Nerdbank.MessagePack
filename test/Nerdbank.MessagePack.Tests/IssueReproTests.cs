using PolyType;
using Xunit;

namespace Nerdbank.MessagePack.Tests;

public partial class IssueReproTests
{
	[Fact]
	public void TestEnumFieldWithInitializer_WithDefaultValue()
	{
		// This test verifies the original issue is resolved when DefaultValue attribute is added
		var original = new TestClassWithDefaultValue();
		original.MyEnum = TestEnum.First;
		var serializer = new MessagePackSerializer();
		var bytes = serializer.Serialize(original);
		var deserialized = serializer.Deserialize<TestClassWithDefaultValue>(bytes);
		Assert.Equal(TestEnum.First, deserialized.MyEnum);
	}

	[GenerateShape]
	public partial class TestClassWithDefaultValue
	{
		[System.ComponentModel.DefaultValue(TestEnum.Second)]
		public TestEnum MyEnum = TestEnum.Second;
	}

	public enum TestEnum
	{
		First = 0,
		Second = 1,
	}
}
