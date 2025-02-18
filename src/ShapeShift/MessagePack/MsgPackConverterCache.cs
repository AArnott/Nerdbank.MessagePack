// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType.Utilities;
using ShapeShift.MessagePack.Converters;

namespace ShapeShift.MessagePack;

/// <summary>
/// A messagepack-specific implementation of <see cref="ConverterCache"/>.
/// </summary>
internal record MsgPackConverterCache : ConverterCache
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MsgPackConverterCache"/> class.
	/// </summary>
	/// <param name="formatter">The formatter.</param>
	/// <param name="deformatter">The deformatter.</param>
	internal MsgPackConverterCache(MsgPackFormatter formatter, MsgPackDeformatter deformatter)
		: base(MsgPackReferencePreservingManager.Instance)
	{
		this.Formatter = formatter;
		this.Deformatter = deformatter;
	}

	/// <summary>
	/// Gets the formatter.
	/// </summary>
	internal MsgPackFormatter Formatter { get; }

	/// <summary>
	/// Gets the deformatter.
	/// </summary>
	internal MsgPackDeformatter Deformatter { get; }

	/// <inheritdoc/>
	protected override StandardVisitor CreateStandardVisitor(TypeGenerationContext context)
		=> new MessagePackVisitor(this, context, this.Formatter, this.Deformatter);
}
