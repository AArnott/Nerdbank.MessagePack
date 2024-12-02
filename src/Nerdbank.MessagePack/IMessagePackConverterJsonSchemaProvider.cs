// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack;

/// <summary>
/// An optional interface that may be implemented by a <see cref="MessagePackConverter{T}"/>-derived type in order to participate in the JSON schema that may be returned from <see cref="MessagePackSerializer.GetJsonSchema(ITypeShape)"/>.
/// </summary>
/// <remarks>
/// Custom converters that do <em>not</em> implement this interface will lead to a JSON schema that does not describe the written data, and allows any data as input.
/// </remarks>
public interface IMessagePackConverterJsonSchemaProvider
{
	/// <summary>
	/// Gets the <see href="https://json-schema.org/">JSON schema</see> that resembles the data structure that this converter can serialize and deserialize.
	/// </summary>
	/// <param name="context">A means to obtain schema fragments for inclusion when your converter delegates to other converters.</param>
	/// <returns>The fragment of JSON schema that describes the value written by this converter.</returns>
	/// <remarks>
	/// <para>
	/// Implementations should return a new instance of <see cref="JsonObject"/> that represents the JSON schema fragment for every caller.
	/// A shared instance <em>may</em> be used to call <see cref="JsonNode.DeepClone"/> and the result returned.
	/// </para>
	/// <para>
	/// If the converter delegates to other converters, the schemas for those sub-values can be obtained for inclusion in the returned schema
	/// by calling <see cref="JsonSchemaContext.GetJsonSchema{T}()"/> on the <paramref name="context"/>.
	/// </para>
	/// </remarks>
	JsonObject GetJsonSchema(JsonSchemaContext context);
}
