// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1649 // File name should match first type name

using Godot;

namespace Nerdbank.MessagePack.Godot;

internal abstract class GodotConverter<T> : MessagePackConverter<T>
{
	protected static void SkipAdditionalElements(ref MessagePackReader reader, SerializationContext context, int actualLength, int expectedLength)
	{
		if (actualLength < expectedLength)
		{
			ThrowInvalidArrayLength(actualLength, expectedLength);
		}

		for (int i = expectedLength; i < actualLength; i++)
		{
			reader.Skip(context);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		[System.Diagnostics.CodeAnalysis.DoesNotReturn]
		static void ThrowInvalidArrayLength(int actualLength, int expectedLength) =>
			throw new MessagePackSerializationException($"Expected array length of at least {expectedLength} but was {actualLength}.");
	}
}

internal sealed class ColorConverter : GodotConverter<Color>
{
	internal static readonly ColorConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Color value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(4);
		writer.Write(value.R);
		writer.Write(value.G);
		writer.Write(value.B);
		writer.Write(value.A);
	}

	public override Color Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		float r = default, g = default, b = default, a = default;
		for (int i = 0; i < Math.Min(length, 4); i++)
		{
			switch (i)
			{
				case 0: r = reader.ReadSingle(); break;
				case 1: g = reader.ReadSingle(); break;
				case 2: b = reader.ReadSingle(); break;
				case 3: a = reader.ReadSingle(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 4);
		return new(r, g, b, a);
	}
}

internal sealed class Vector2Converter : GodotConverter<Vector2>
{
	internal static readonly Vector2Converter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Vector2 value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(2);
		writer.Write(value.X);
		writer.Write(value.Y);
	}

	public override Vector2 Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		float x = default, y = default;
		for (int i = 0; i < Math.Min(length, 2); i++)
		{
			switch (i)
			{
				case 0: x = reader.ReadSingle(); break;
				case 1: y = reader.ReadSingle(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 2);
		return new(x, y);
	}
}

internal sealed class Vector2IConverter : GodotConverter<Vector2I>
{
	internal static readonly Vector2IConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Vector2I value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(2);
		writer.Write(value.X);
		writer.Write(value.Y);
	}

	public override Vector2I Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		int x = default, y = default;
		for (int i = 0; i < Math.Min(length, 2); i++)
		{
			switch (i)
			{
				case 0: x = reader.ReadInt32(); break;
				case 1: y = reader.ReadInt32(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 2);
		return new(x, y);
	}
}

internal sealed class Vector3Converter : GodotConverter<Vector3>
{
	internal static readonly Vector3Converter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Vector3 value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(3);
		writer.Write(value.X);
		writer.Write(value.Y);
		writer.Write(value.Z);
	}

	public override Vector3 Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		float x = default, y = default, z = default;
		for (int i = 0; i < Math.Min(length, 3); i++)
		{
			switch (i)
			{
				case 0: x = reader.ReadSingle(); break;
				case 1: y = reader.ReadSingle(); break;
				case 2: z = reader.ReadSingle(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 3);
		return new(x, y, z);
	}
}

internal sealed class Vector3IConverter : GodotConverter<Vector3I>
{
	internal static readonly Vector3IConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Vector3I value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(3);
		writer.Write(value.X);
		writer.Write(value.Y);
		writer.Write(value.Z);
	}

	public override Vector3I Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		int x = default, y = default, z = default;
		for (int i = 0; i < Math.Min(length, 3); i++)
		{
			switch (i)
			{
				case 0: x = reader.ReadInt32(); break;
				case 1: y = reader.ReadInt32(); break;
				case 2: z = reader.ReadInt32(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 3);
		return new(x, y, z);
	}
}

internal sealed class Vector4Converter : GodotConverter<Vector4>
{
	internal static readonly Vector4Converter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Vector4 value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(4);
		writer.Write(value.X);
		writer.Write(value.Y);
		writer.Write(value.Z);
		writer.Write(value.W);
	}

	public override Vector4 Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		float x = default, y = default, z = default, w = default;
		for (int i = 0; i < Math.Min(length, 4); i++)
		{
			switch (i)
			{
				case 0: x = reader.ReadSingle(); break;
				case 1: y = reader.ReadSingle(); break;
				case 2: z = reader.ReadSingle(); break;
				case 3: w = reader.ReadSingle(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 4);
		return new(x, y, z, w);
	}
}

internal sealed class Vector4IConverter : GodotConverter<Vector4I>
{
	internal static readonly Vector4IConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Vector4I value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(4);
		writer.Write(value.X);
		writer.Write(value.Y);
		writer.Write(value.Z);
		writer.Write(value.W);
	}

	public override Vector4I Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		int x = default, y = default, z = default, w = default;
		for (int i = 0; i < Math.Min(length, 4); i++)
		{
			switch (i)
			{
				case 0: x = reader.ReadInt32(); break;
				case 1: y = reader.ReadInt32(); break;
				case 2: z = reader.ReadInt32(); break;
				case 3: w = reader.ReadInt32(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 4);
		return new(x, y, z, w);
	}
}

internal sealed class Rect2Converter : GodotConverter<Rect2>
{
	internal static readonly Rect2Converter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Rect2 value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(4);
		writer.Write(value.Position.X);
		writer.Write(value.Position.Y);
		writer.Write(value.Size.X);
		writer.Write(value.Size.Y);
	}

	public override Rect2 Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		float x = default, y = default, width = default, height = default;
		for (int i = 0; i < Math.Min(length, 4); i++)
		{
			switch (i)
			{
				case 0: x = reader.ReadSingle(); break;
				case 1: y = reader.ReadSingle(); break;
				case 2: width = reader.ReadSingle(); break;
				case 3: height = reader.ReadSingle(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 4);
		return new(x, y, width, height);
	}
}

internal sealed class Rect2IConverter : GodotConverter<Rect2I>
{
	internal static readonly Rect2IConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Rect2I value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(4);
		writer.Write(value.Position.X);
		writer.Write(value.Position.Y);
		writer.Write(value.Size.X);
		writer.Write(value.Size.Y);
	}

	public override Rect2I Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		int x = default, y = default, width = default, height = default;
		for (int i = 0; i < Math.Min(length, 4); i++)
		{
			switch (i)
			{
				case 0: x = reader.ReadInt32(); break;
				case 1: y = reader.ReadInt32(); break;
				case 2: width = reader.ReadInt32(); break;
				case 3: height = reader.ReadInt32(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 4);
		return new(x, y, width, height);
	}
}

internal sealed class QuaternionConverter : GodotConverter<Quaternion>
{
	internal static readonly QuaternionConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Quaternion value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(4);
		writer.Write(value.X);
		writer.Write(value.Y);
		writer.Write(value.Z);
		writer.Write(value.W);
	}

	public override Quaternion Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		float x = default, y = default, z = default, w = default;
		for (int i = 0; i < Math.Min(length, 4); i++)
		{
			switch (i)
			{
				case 0: x = reader.ReadSingle(); break;
				case 1: y = reader.ReadSingle(); break;
				case 2: z = reader.ReadSingle(); break;
				case 3: w = reader.ReadSingle(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 4);
		return new(x, y, z, w);
	}
}

internal sealed class PlaneConverter : GodotConverter<Plane>
{
	internal static readonly PlaneConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Plane value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(4);
		writer.Write(value.Normal.X);
		writer.Write(value.Normal.Y);
		writer.Write(value.Normal.Z);
		writer.Write(value.D);
	}

	public override Plane Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		float x = default, y = default, z = default, d = default;
		for (int i = 0; i < Math.Min(length, 4); i++)
		{
			switch (i)
			{
				case 0: x = reader.ReadSingle(); break;
				case 1: y = reader.ReadSingle(); break;
				case 2: z = reader.ReadSingle(); break;
				case 3: d = reader.ReadSingle(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 4);
		return new(x, y, z, d);
	}
}

internal sealed class AabbConverter : GodotConverter<Aabb>
{
	internal static readonly AabbConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Aabb value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(6);
		writer.Write(value.Position.X);
		writer.Write(value.Position.Y);
		writer.Write(value.Position.Z);
		writer.Write(value.Size.X);
		writer.Write(value.Size.Y);
		writer.Write(value.Size.Z);
	}

	public override Aabb Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		float px = default, py = default, pz = default, sx = default, sy = default, sz = default;
		for (int i = 0; i < Math.Min(length, 6); i++)
		{
			switch (i)
			{
				case 0: px = reader.ReadSingle(); break;
				case 1: py = reader.ReadSingle(); break;
				case 2: pz = reader.ReadSingle(); break;
				case 3: sx = reader.ReadSingle(); break;
				case 4: sy = reader.ReadSingle(); break;
				case 5: sz = reader.ReadSingle(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 6);
		return new(new Vector3(px, py, pz), new Vector3(sx, sy, sz));
	}
}

internal sealed class Transform2DConverter : GodotConverter<Transform2D>
{
	internal static readonly Transform2DConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Transform2D value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(6);
		writer.Write(value.X.X);
		writer.Write(value.X.Y);
		writer.Write(value.Y.X);
		writer.Write(value.Y.Y);
		writer.Write(value.Origin.X);
		writer.Write(value.Origin.Y);
	}

	public override Transform2D Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		float xx = default, xy = default, yx = default, yy = default, ox = default, oy = default;
		for (int i = 0; i < Math.Min(length, 6); i++)
		{
			switch (i)
			{
				case 0: xx = reader.ReadSingle(); break;
				case 1: xy = reader.ReadSingle(); break;
				case 2: yx = reader.ReadSingle(); break;
				case 3: yy = reader.ReadSingle(); break;
				case 4: ox = reader.ReadSingle(); break;
				case 5: oy = reader.ReadSingle(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 6);
		return new(xx, xy, yx, yy, ox, oy);
	}
}

internal sealed class BasisConverter : GodotConverter<Basis>
{
	internal static readonly BasisConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Basis value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(9);
		writer.Write(value.X.X);
		writer.Write(value.X.Y);
		writer.Write(value.X.Z);
		writer.Write(value.Y.X);
		writer.Write(value.Y.Y);
		writer.Write(value.Y.Z);
		writer.Write(value.Z.X);
		writer.Write(value.Z.Y);
		writer.Write(value.Z.Z);
	}

	public override Basis Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		float xx = default, xy = default, xz = default, yx = default, yy = default, yz = default, zx = default, zy = default, zz = default;
		for (int i = 0; i < Math.Min(length, 9); i++)
		{
			switch (i)
			{
				case 0: xx = reader.ReadSingle(); break;
				case 1: xy = reader.ReadSingle(); break;
				case 2: xz = reader.ReadSingle(); break;
				case 3: yx = reader.ReadSingle(); break;
				case 4: yy = reader.ReadSingle(); break;
				case 5: yz = reader.ReadSingle(); break;
				case 6: zx = reader.ReadSingle(); break;
				case 7: zy = reader.ReadSingle(); break;
				case 8: zz = reader.ReadSingle(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 9);
		return new(new Vector3(xx, xy, xz), new Vector3(yx, yy, yz), new Vector3(zx, zy, zz));
	}
}

internal sealed class Transform3DConverter : GodotConverter<Transform3D>
{
	internal static readonly Transform3DConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Transform3D value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(12);
		writer.Write(value.Basis.X.X);
		writer.Write(value.Basis.X.Y);
		writer.Write(value.Basis.X.Z);
		writer.Write(value.Basis.Y.X);
		writer.Write(value.Basis.Y.Y);
		writer.Write(value.Basis.Y.Z);
		writer.Write(value.Basis.Z.X);
		writer.Write(value.Basis.Z.Y);
		writer.Write(value.Basis.Z.Z);
		writer.Write(value.Origin.X);
		writer.Write(value.Origin.Y);
		writer.Write(value.Origin.Z);
	}

	public override Transform3D Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		float xx = default, xy = default, xz = default, yx = default, yy = default, yz = default, zx = default, zy = default, zz = default, ox = default, oy = default, oz = default;
		for (int i = 0; i < Math.Min(length, 12); i++)
		{
			switch (i)
			{
				case 0: xx = reader.ReadSingle(); break;
				case 1: xy = reader.ReadSingle(); break;
				case 2: xz = reader.ReadSingle(); break;
				case 3: yx = reader.ReadSingle(); break;
				case 4: yy = reader.ReadSingle(); break;
				case 5: yz = reader.ReadSingle(); break;
				case 6: zx = reader.ReadSingle(); break;
				case 7: zy = reader.ReadSingle(); break;
				case 8: zz = reader.ReadSingle(); break;
				case 9: ox = reader.ReadSingle(); break;
				case 10: oy = reader.ReadSingle(); break;
				case 11: oz = reader.ReadSingle(); break;
			}
		}

		SkipAdditionalElements(ref reader, context, length, 12);
		return new(new Basis(new Vector3(xx, xy, xz), new Vector3(yx, yy, yz), new Vector3(zx, zy, zz)), new Vector3(ox, oy, oz));
	}
}

internal sealed class ProjectionConverter : GodotConverter<Projection>
{
	internal static readonly ProjectionConverter Instance = new();

	public override void Write(ref MessagePackWriter writer, in Projection value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(16);
		writer.Write(value.X.X);
		writer.Write(value.X.Y);
		writer.Write(value.X.Z);
		writer.Write(value.X.W);
		writer.Write(value.Y.X);
		writer.Write(value.Y.Y);
		writer.Write(value.Y.Z);
		writer.Write(value.Y.W);
		writer.Write(value.Z.X);
		writer.Write(value.Z.Y);
		writer.Write(value.Z.Z);
		writer.Write(value.Z.W);
		writer.Write(value.W.X);
		writer.Write(value.W.Y);
		writer.Write(value.W.Z);
		writer.Write(value.W.W);
	}

	public override Projection Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		int length = reader.ReadArrayHeader();
		Span<float> values = stackalloc float[16];
		for (int i = 0; i < Math.Min(length, values.Length); i++)
		{
			values[i] = reader.ReadSingle();
		}

		SkipAdditionalElements(ref reader, context, length, values.Length);
		return new(new Vector4(values[0], values[1], values[2], values[3]), new Vector4(values[4], values[5], values[6], values[7]), new Vector4(values[8], values[9], values[10], values[11]), new Vector4(values[12], values[13], values[14], values[15]));
	}
}
