// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Godot;
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.Godot;
using PolyType;
using PolyType.ReflectionProvider;
using Xunit;

public partial class GodotConverterTests
{
	private static readonly MessagePackSerializer Serializer = new MessagePackSerializer().WithGodotConverters();

	[Fact]
	public void ValueTypes_RoundTrip()
	{
		Assert.Equal(new Color(1, 2, 3, 4), RoundTrip(new Color(1, 2, 3, 4)));
		Assert.Equal(new Vector2(1, 2), RoundTrip(new Vector2(1, 2)));
		Assert.Equal(new Vector2I(1, 2), RoundTrip(new Vector2I(1, 2)));
		Assert.Equal(new Vector3(1, 2, 3), RoundTrip(new Vector3(1, 2, 3)));
		Assert.Equal(new Vector3I(1, 2, 3), RoundTrip(new Vector3I(1, 2, 3)));
		Assert.Equal(new Vector4(1, 2, 3, 4), RoundTrip(new Vector4(1, 2, 3, 4)));
		Assert.Equal(new Vector4I(1, 2, 3, 4), RoundTrip(new Vector4I(1, 2, 3, 4)));
		Assert.Equal(new Rect2(1, 2, 3, 4), RoundTrip(new Rect2(1, 2, 3, 4)));
		Assert.Equal(new Rect2I(1, 2, 3, 4), RoundTrip(new Rect2I(1, 2, 3, 4)));
		Assert.Equal(new Quaternion(1, 2, 3, 4), RoundTrip(new Quaternion(1, 2, 3, 4)));
		Assert.Equal(new Plane(1, 2, 3, 4), RoundTrip(new Plane(1, 2, 3, 4)));
		Assert.Equal(new Aabb(new Vector3(1, 2, 3), new Vector3(4, 5, 6)), RoundTrip(new Aabb(new Vector3(1, 2, 3), new Vector3(4, 5, 6))));
		Assert.Equal(new Transform2D(1, 2, 3, 4, 5, 6), RoundTrip(new Transform2D(1, 2, 3, 4, 5, 6)));

		Basis basis = new(new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9));
		Assert.Equal(basis, RoundTrip(basis));
		Transform3D transform = new(basis, new Vector3(10, 11, 12));
		Assert.Equal(transform, RoundTrip(transform));
		Projection projection = new(new Vector4(1, 2, 3, 4), new Vector4(5, 6, 7, 8), new Vector4(9, 10, 11, 12), new Vector4(13, 14, 15, 16));
		Assert.Equal(projection, RoundTrip(projection));
	}

	[Fact]
	public void Vector2_UsesMessagePackGodotWireFormat()
	{
		byte[] serialized = Serializer.Serialize<Vector2, GodotShapes>(new Vector2(1.5f, -2.5f), TestContext.Current.CancellationToken);
		Assert.Equal([0x92, 0xca, 0x3f, 0xc0, 0x00, 0x00, 0xca, 0xc0, 0x20, 0x00, 0x00], serialized);
	}

	[Fact]
	public void Vector2I_UsesMessagePackGodotWireFormat()
	{
		byte[] serialized = Serializer.Serialize<Vector2I, GodotShapes>(new Vector2I(1, -2), TestContext.Current.CancellationToken);
		Assert.Equal([0x92, 0x01, 0xfe], serialized);
	}

	[Fact]
	public void Transform3D_UsesMessagePackGodotWireFormat()
	{
		Transform3D value = new(
			new Basis(new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9)),
			new Vector3(10, 11, 12));

		byte[] serialized = Serializer.Serialize<Transform3D, GodotShapes>(value, TestContext.Current.CancellationToken);

		Assert.Equal(
		[
			0x9c,
			0xca, 0x3f, 0x80, 0x00, 0x00, 0xca, 0x40, 0x00, 0x00, 0x00, 0xca, 0x40, 0x40, 0x00, 0x00,
			0xca, 0x40, 0x80, 0x00, 0x00, 0xca, 0x40, 0xa0, 0x00, 0x00, 0xca, 0x40, 0xc0, 0x00, 0x00,
			0xca, 0x40, 0xe0, 0x00, 0x00, 0xca, 0x41, 0x00, 0x00, 0x00, 0xca, 0x41, 0x10, 0x00, 0x00,
			0xca, 0x41, 0x20, 0x00, 0x00, 0xca, 0x41, 0x30, 0x00, 0x00, 0xca, 0x41, 0x40, 0x00, 0x00,
		],
		serialized);
	}

	[Fact]
	public void Vector2_IgnoresAdditionalArrayElements()
	{
		Vector2 result = Serializer.Deserialize<Vector2, GodotShapes>(new byte[] { 0x93, 0xca, 0x3f, 0x80, 0x00, 0x00, 0xca, 0x40, 0x00, 0x00, 0x00, 42 }, TestContext.Current.CancellationToken);
		Assert.Equal(new Vector2(1, 2), result);
	}

	[Fact]
	public void Vector2_RejectsMissingArrayElements()
	{
		Assert.Throws<MessagePackSerializationException>(
			() => Serializer.Deserialize<Vector2, GodotShapes>(new byte[] { 0x91, 0xca, 0x3f, 0x80, 0x00, 0x00 }, TestContext.Current.CancellationToken));
	}

	private static T RoundTrip<T>(T value)
	{
		ITypeShape<T> shape = ReflectionTypeShapeProvider.Default.GetTypeShapeOrThrow<T>();
		return Serializer.Deserialize(Serializer.Serialize(value, shape, TestContext.Current.CancellationToken), shape, TestContext.Current.CancellationToken)!;
	}

	[GenerateShapeFor<Aabb>]
	[GenerateShapeFor<Basis>]
	[GenerateShapeFor<Color>]
	[GenerateShapeFor<Plane>]
	[GenerateShapeFor<Projection>]
	[GenerateShapeFor<Quaternion>]
	[GenerateShapeFor<Rect2>]
	[GenerateShapeFor<Rect2I>]
	[GenerateShapeFor<Transform2D>]
	[GenerateShapeFor<Transform3D>]
	[GenerateShapeFor<Vector2>]
	[GenerateShapeFor<Vector2I>]
	[GenerateShapeFor<Vector3>]
	[GenerateShapeFor<Vector3I>]
	[GenerateShapeFor<Vector4>]
	[GenerateShapeFor<Vector4I>]
	private partial class GodotShapes;
}
