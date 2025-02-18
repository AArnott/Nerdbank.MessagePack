// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ShapeShift.MessagePack.Converters;
using PolyType.Utilities;

namespace ShapeShift.MessagePack;

internal record MsgPackConverterCache : ConverterCache
{
	internal MsgPackConverterCache(MsgPackFormatter formatter, MsgPackDeformatter deformatter)
		: base(MsgPackReferencePreservingManager.Instance)
	{
		this.Formatter = formatter;
		this.Deformatter = deformatter;
	}

	internal MsgPackFormatter Formatter { get; }

	internal MsgPackDeformatter Deformatter { get; }

	protected override StandardVisitor CreateStandardVisitor(TypeGenerationContext context)
		=> new MessagePackVisitor(this, context, this.Formatter, this.Deformatter);
}
