// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Godot;
using PolyType;

namespace Nerdbank.MessagePack.Godot;

/// <summary>
/// Extension methods for configuring <see cref="MessagePackSerializer"/> to serialize Godot value types.
/// </summary>
public static class GodotMessagePackSerializerExtensions
{
	/// <summary>
	/// Creates a copy of a serializer configured to serialize Godot Engine value types.
	/// </summary>
	/// <param name="serializer">The serializer to configure.</param>
	/// <returns>A serializer configured with converters for Godot Engine value types.</returns>
	public static MessagePackSerializer WithGodotConverters(this MessagePackSerializer serializer)
	{
		ArgumentNullException.ThrowIfNull(serializer);
		return serializer with { ConverterFactories = [.. serializer.ConverterFactories, GodotConverterFactory.Instance] };
	}

	private sealed class GodotConverterFactory : IMessagePackConverterFactory
	{
		internal static readonly GodotConverterFactory Instance = new();

		public MessagePackConverter? CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
			=> type == typeof(Aabb) ? AabbConverter.Instance
			: type == typeof(Basis) ? BasisConverter.Instance
			: type == typeof(Color) ? ColorConverter.Instance
			: type == typeof(Plane) ? PlaneConverter.Instance
			: type == typeof(Projection) ? ProjectionConverter.Instance
			: type == typeof(Quaternion) ? QuaternionConverter.Instance
			: type == typeof(Rect2) ? Rect2Converter.Instance
			: type == typeof(Rect2I) ? Rect2IConverter.Instance
			: type == typeof(Transform2D) ? Transform2DConverter.Instance
			: type == typeof(Transform3D) ? Transform3DConverter.Instance
			: type == typeof(Vector2) ? Vector2Converter.Instance
			: type == typeof(Vector2I) ? Vector2IConverter.Instance
			: type == typeof(Vector3) ? Vector3Converter.Instance
			: type == typeof(Vector3I) ? Vector3IConverter.Instance
			: type == typeof(Vector4) ? Vector4Converter.Instance
			: type == typeof(Vector4I) ? Vector4IConverter.Instance
			: null;
	}
}
