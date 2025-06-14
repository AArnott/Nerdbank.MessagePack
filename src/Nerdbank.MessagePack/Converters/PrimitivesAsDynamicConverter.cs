﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A converter that can write msgpack primitives or other convertible values typed as <see cref="object"/>
/// and reads everything into primitives, dictionaries and arrays.
/// </summary>
/// <remarks>
/// <para>
/// This converter is not included by default because untyped serialization is not generally desirable.
/// But it is offered as a converter that may be added to <see cref="MessagePackSerializer.Converters"/>
/// in order to enable limited untyped serialization.
/// </para>
/// <para>
/// This converter is very similar to <see cref="PrimitivesAsObjectConverter"/>.
/// What distinguishes this class is that the result can be used with the C# <c>dynamic</c> keyword
/// to index into maps using string keys as if they were properties.
/// </para>
/// </remarks>
internal class PrimitivesAsDynamicConverter : PrimitivesAsObjectConverter
{
	/// <summary>
	/// Gets the default instance of the converter.
	/// </summary>
	internal static new readonly PrimitivesAsDynamicConverter Instance = new();

	/// <inheritdoc/>
	protected override IReadOnlyDictionary<object, object?> WrapDictionary(IReadOnlyDictionary<object, object?> content)
		=> new DynamicMsgPackDictionary(content ?? throw new ArgumentNullException(nameof(content)));
}
