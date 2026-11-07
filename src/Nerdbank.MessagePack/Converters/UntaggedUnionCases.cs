// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Selects untagged C# union cases using native MessagePack encodings.
/// </summary>
/// <param name="preserveReferences">Whether payload converters preserve references, including delayed converters.</param>
/// <param name="allowNonNullableNull">Whether nil may select a non-nullable reference case when no nullable case is available.</param>
internal sealed class UntaggedUnionCases(bool preserveReferences, bool allowNonNullableNull)
{
	private readonly Dictionary<MessagePackType, MessagePackConverter> convertersByType = [];
	private readonly Dictionary<byte, MessagePackConverter> convertersByCode = [];
	private readonly List<(byte Code, long Min, ulong Max, MessagePackConverter Converter, CSharpUnionCase Case)> integers = [];
	private readonly HashSet<MessagePackConverter> converters = [];
	private readonly Dictionary<MessagePackConverter, Func<object, bool>> nullPayloadTests = [];
	private readonly Dictionary<Type, MessagePackConverter> referenceConverters = [];
	private MessagePackConverter? nilConverter;
	private MessagePackConverter? firstReferenceConverter;
	private int collectionCount;

	/// <summary>Gets a value indicating whether an array case is untagged.</summary>
	internal bool HasArray => this.convertersByType.ContainsKey(MessagePackType.Array);

	/// <summary>Gets a value indicating whether a map case is untagged.</summary>
	internal bool HasMap => this.convertersByType.ContainsKey(MessagePackType.Map);

	/// <summary>
	/// Registers a case if its built-in encoding can be identified without a discriminator.
	/// </summary>
	/// <param name="shape">The payload shape.</param>
	/// <param name="converter">The marshaling case converter.</param>
	/// <param name="caseInfo">The payload converter and integer encoding options.</param>
	/// <param name="isNullable">Whether the union case admits a null payload.</param>
	internal void Add(ITypeShape shape, MessagePackConverter converter, CSharpUnionCase caseInfo, bool isNullable)
	{
		MessagePackConverter payload = ((IMessagePackConverterInternal)caseInfo.PayloadConverter!).UnwrapReferencePreservation();
		if (payload.GetType().Assembly != typeof(UntaggedUnionCases).Assembly)
		{
			// A custom converter need not use the payload type's normal wire encoding.
			return;
		}

		Type type = shape.Type;
		(byte Code, long Min, ulong Max)? integer = Type.GetTypeCode(type) switch
		{
			TypeCode.SByte => (MessagePackCode.Int8, sbyte.MinValue, (ulong)sbyte.MaxValue),
			TypeCode.Byte => (MessagePackCode.UInt8, 0, byte.MaxValue),
			TypeCode.Int16 => (MessagePackCode.Int16, short.MinValue, (ulong)short.MaxValue),
			TypeCode.UInt16 => (MessagePackCode.UInt16, 0, ushort.MaxValue),
			TypeCode.Char => (MessagePackCode.UInt16, 0, char.MaxValue),
			TypeCode.Int32 => (MessagePackCode.Int32, int.MinValue, int.MaxValue),
			TypeCode.UInt32 => (MessagePackCode.UInt32, 0, uint.MaxValue),
			TypeCode.Int64 => (MessagePackCode.Int64, long.MinValue, long.MaxValue),
			TypeCode.UInt64 => (MessagePackCode.UInt64, 0, ulong.MaxValue),
			_ => null,
		};
		if (integer is { } number && !type.IsEnum)
		{
			if (this.convertersByCode.ContainsKey(number.Code))
			{
				throw new NotSupportedException("Multiple untagged C# union cases use the same integer encoding. Assign an explicit alias to distinguish them.");
			}

			this.integers.Add((number.Code, number.Min, number.Max, converter, caseInfo));
			this.convertersByCode.Add(number.Code, converter);
			this.converters.Add(converter);
			return;
		}

		MessagePackType wireType;
		if (type == typeof(float) || type == typeof(double))
		{
			this.convertersByCode.Add(type == typeof(float) ? MessagePackCode.Float32 : MessagePackCode.Float64, converter);
			this.converters.Add(converter);
			return;
		}
		else if (type == typeof(bool))
		{
			wireType = MessagePackType.Boolean;
		}
		else if (type == typeof(string))
		{
			wireType = MessagePackType.String;
		}
		else if (type == typeof(byte[]) || type == typeof(Memory<byte>) || type == typeof(ReadOnlyMemory<byte>))
		{
			wireType = MessagePackType.Binary;
		}
		else if (shape is IEnumerableTypeShape)
		{
			wireType = MessagePackType.Array;
		}
		else if (shape is IDictionaryTypeShape)
		{
			wireType = MessagePackType.Map;
		}
		else
		{
			return;
		}

		if (wireType is MessagePackType.Array or MessagePackType.Map or MessagePackType.Binary && ++this.collectionCount > 1)
		{
			throw new NotSupportedException("A C# union cannot have more than one untagged collection case. Assign explicit aliases to disambiguate the collection cases.");
		}

		if (!type.IsValueType)
		{
			this.firstReferenceConverter ??= converter;
			if (isNullable)
			{
				this.nilConverter ??= converter;
			}

			if (preserveReferences)
			{
				this.referenceConverters.Add(type, converter);
			}
		}

		if (caseInfo.IsNullPayload is { } isNullPayload)
		{
			this.nullPayloadTests.Add(converter, isNullPayload);
		}

		this.convertersByType.Add(wireType, converter);
		this.converters.Add(converter);
	}

	/// <summary>Enables exact-width writes when several untagged integer cases must be distinguished.</summary>
	internal void Complete()
	{
		this.integers.Sort((a, b) => a.Max.CompareTo(b.Max));
		foreach ((_, _, _, _, CSharpUnionCase caseInfo) in this.integers)
		{
			caseInfo.UseFixedWidthIntegers = this.integers.Count > 1;
		}
	}

	/// <summary>Determines whether a case is written without a discriminator.</summary>
	/// <param name="converter">The case converter.</param>
	/// <returns>Whether the converter is untagged.</returns>
	internal bool Contains(MessagePackConverter converter) => this.converters.Contains(converter);

	/// <summary>Determines whether a case is selected for a nil value.</summary>
	/// <param name="converter">The case converter.</param>
	/// <returns>Whether a nil value selects the converter.</returns>
	internal bool CanSelectNil(MessagePackConverter converter)
		=> converter == this.nilConverter || (this.nilConverter is null && allowNonNullableNull && converter == this.firstReferenceConverter);

	/// <summary>Determines whether an untagged case contains a null payload requiring a discriminator.</summary>
	/// <param name="converter">The case converter.</param>
	/// <param name="value">The non-null union instance.</param>
	/// <returns>Whether a discriminator is required to preserve the containing union instance.</returns>
	internal bool HasNullPayload(MessagePackConverter converter, object value) => this.nullPayloadTests.TryGetValue(converter, out Func<object, bool>? test) && test(value);

	/// <summary>
	/// Selects a converter without consuming the value.
	/// </summary>
	/// <param name="reader">The reader at the start of the value.</param>
	/// <param name="allowNilCase">Whether nil should construct a case instead of a null union reference.</param>
	/// <param name="context">The serialization context, including reference extension codes.</param>
	/// <returns>The matching case converter, or null if a discriminator is required.</returns>
	internal MessagePackConverter? Select(in MessagePackReader reader, bool allowNilCase, in SerializationContext context)
	{
		byte code = reader.NextCode;
		if (code == MessagePackCode.Nil)
		{
			return allowNilCase
				? this.nilConverter ?? (allowNonNullableNull ? this.firstReferenceConverter : null)
					?? throw new MessagePackSerializationException("No nullable untagged C# union case accepts nil. Null object payloads require a discriminator; non-nullable reference cases require AllowNullValuesForNonNullableProperties.")
				: null;
		}

		if (this.convertersByCode.TryGetValue(code, out MessagePackConverter? exact))
		{
			return exact;
		}

		if (reader.NextMessagePackType == MessagePackType.Extension && this.referenceConverters.Count > 0)
		{
			MessagePackReader peek = reader.CreatePeekReader();
			if (peek.ReadExtensionHeader().TypeCode == context.ExtensionTypeCodes.ObjectReference)
			{
				object? referencedObject = context.ReferenceEqualityTracker!.GetDeserializedObject(peek.ReadInt32());
				foreach (KeyValuePair<Type, MessagePackConverter> candidate in this.referenceConverters)
				{
					if (candidate.Key.IsInstanceOfType(referencedObject))
					{
						return candidate.Value;
					}
				}

				throw new MessagePackSerializationException("The referenced object does not match an untagged C# union case.");
			}
		}

		if (reader.NextMessagePackType == MessagePackType.Integer && this.integers.Count > 0)
		{
			MessagePackReader peek = reader.CreatePeekReader();
			if (code == MessagePackCode.UInt64)
			{
				ulong value = peek.ReadUInt64();
				foreach ((_, _, ulong max, MessagePackConverter integerConverter, _) in this.integers)
				{
					if (value <= max)
					{
						return integerConverter;
					}
				}
			}
			else
			{
				long value = peek.ReadInt64();
				foreach ((_, long min, ulong max, MessagePackConverter integerConverter, _) in this.integers)
				{
					if (value >= min && (value < 0 || (ulong)value <= max))
					{
						return integerConverter;
					}
				}
			}

			throw new MessagePackSerializationException("The integer does not fit any untagged C# union case.");
		}

		return this.convertersByType.TryGetValue(reader.NextMessagePackType, out MessagePackConverter? converter) ? converter : null;
	}
}
