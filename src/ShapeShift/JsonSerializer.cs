// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using ShapeShift.Json;

namespace ShapeShift;

/// <summary>
/// Serializes .NET objects using the JSON format.
/// </summary>
/// <devremarks>
/// <para>
/// This class may declare properties that customize how JSON serialization is performed.
/// These properties must use <see langword="init"/> accessors to prevent modification after construction,
/// since there is no means to replace converters once they are created.
/// </para>
/// </devremarks>
public record JsonSerializer : SerializerBase
{
	/// <summary>
	/// Initializes a new instance of the <see cref="JsonSerializer"/> class.
	/// </summary>
	public JsonSerializer()
		: base(new JsonConverterCache(JsonFormatter.Default, new Deformatter(JsonStreamingDeformatter.Default)))
	{
	}

	/// <summary>
	/// Gets the encoding used to format JSON.
	/// </summary>
	public Encoding Encoding => this.ConverterCache.Formatter.Encoding;
}
