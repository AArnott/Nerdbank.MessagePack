// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType.Utilities;

namespace ShapeShift.Json;

/// <summary>
/// A JSON-specific implementation of <see cref="ConverterCache"/>.
/// </summary>
internal record JsonConverterCache : ConverterCache
{
	/// <summary>
	/// Initializes a new instance of the <see cref="JsonConverterCache"/> class.
	/// </summary>
	/// <param name="formatter">The formatter.</param>
	/// <param name="deformatter">The deformatter.</param>
	internal JsonConverterCache(JsonFormatter formatter, Deformatter deformatter)
		: base((IReferencePreservingManager?)null) // TODO: implement reference preservation
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
		=> new JsonVisitor(this, context);
}
