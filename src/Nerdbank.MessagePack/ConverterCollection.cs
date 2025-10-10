// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nerdbank.MessagePack;

/// <summary>
/// An immutable collection of converters.
/// </summary>
[CollectionBuilder(typeof(ConverterCollection), nameof(Create))]
public class ConverterCollection : IReadOnlyCollection<MessagePackConverter>
{
	private ConverterCollection(FrozenDictionary<Type, MessagePackConverter> map)
	{
		this.Map = map;
	}

	/// <inheritdoc/>
	public int Count => this.Map.Count;

	private FrozenDictionary<Type, MessagePackConverter> Map { get; }

	/// <summary>
	/// Creates a new instance of <see cref="ConverterCollection"/> from the specified converters.
	/// </summary>
	/// <param name="converters">The converters to fill the collection with.</param>
	/// <returns>The initialized collection.</returns>
	public static ConverterCollection Create(ReadOnlySpan<MessagePackConverter> converters)
	{
		Dictionary<Type, MessagePackConverter> map = [];
		foreach (MessagePackConverter converter in converters)
		{
			map.Add(converter.DataType, converter);
		}

		return new(map.ToFrozenDictionary());
	}

	/// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
	public ImmutableArray<MessagePackConverter>.Enumerator GetEnumerator() => this.Map.Values.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator<MessagePackConverter> IEnumerable<MessagePackConverter>.GetEnumerator() => ((IReadOnlyList<MessagePackConverter>)this.Map.Values).GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<MessagePackConverter>)this).GetEnumerator();

	/// <summary>
	/// Retrieves a converter for a given data type, if the user supplied one.
	/// </summary>
	/// <param name="dataType">The data type.</param>
	/// <param name="converter">Receives the converter, if available.</param>
	/// <returns>A value indicating whether a converter was available.</returns>
	internal bool TryGetConverter(Type dataType, [NotNullWhen(true)] out MessagePackConverter? converter)
	{
		if (this.Map.TryGetValue(dataType, out converter))
		{
			return true;
		}

		converter = default;
		return false;
	}
}
