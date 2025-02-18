// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType.Utilities;
using ShapeShift.MessagePack.Converters;

namespace ShapeShift.MessagePack;

/// <summary>
/// A messagepack-specific implementation of <see cref="ConverterCache"/>.
/// </summary>
internal record MessagePackConverterCache : ConverterCache
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackConverterCache"/> class.
	/// </summary>
	/// <param name="formatter">The formatter.</param>
	/// <param name="deformatter">The deformatter.</param>
	internal MessagePackConverterCache(MessagePackFormatter formatter, MessagePackDeformatter deformatter)
		: base(MsgPackReferencePreservingManager.Instance)
	{
		this.Formatter = formatter;
		this.Deformatter = deformatter;
	}

	/// <summary>
	/// Gets the formatter.
	/// </summary>
	internal override Formatter Formatter { get; }

	/// <summary>
	/// Gets the deformatter.
	/// </summary>
	internal override Deformatter Deformatter { get; }

	/// <inheritdoc/>
	protected override StandardVisitor CreateStandardVisitor(TypeGenerationContext context)
		=> new MessagePackVisitor(this, context);
}
