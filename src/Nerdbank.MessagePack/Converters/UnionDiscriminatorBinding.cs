// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // The binding and its private readers share one file.
#pragma warning disable SA1204 // Static helpers sit next to the checks that use them.

#if NET
using System.Diagnostics.CodeAnalysis;
#endif
using System.Globalization;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// The constant discriminator an object converter always writes and optionally checks on read.
/// </summary>
internal sealed class UnionDiscriminatorBinding
{
	private readonly ReadOnlyMemory<byte> utf8Name;
	private readonly ReadOnlyMemory<byte> msgpackName;
	private readonly object expected;
	private readonly MessagePackConverter? converter;
	private readonly bool enumByName;
	private readonly string schemaType;
	private readonly object schemaValue;
	private readonly byte[] valueBytes;

	private UnionDiscriminatorBinding(string name, IPropertyShape? source, int? keyIndex, object expected, MessagePackConverter? converter, bool enumByName)
	{
		this.Name = name;
		this.Source = source;
		this.KeyIndex = keyIndex;
		this.expected = expected;
		this.converter = converter;
		this.enumByName = enumByName;
		StringEncoding.GetEncodedStringBytes(name, out this.utf8Name, out this.msgpackName);
		this.valueBytes = CaptureValue(this);
		(this.schemaType, this.schemaValue) = SchemaFromCaptured(this.valueBytes, expected);
	}

	/// <summary>Gets the serialized map key.</summary>
	internal string Name { get; }

	/// <summary>Gets the member this discriminator reads, or <see langword="null"/> for the type-level form.</summary>
	internal IPropertyShape? Source { get; }

	/// <summary>Gets the keyed-layout index, or <see langword="null"/> when the value is a name-keyed map.</summary>
	internal int? KeyIndex { get; }

	/// <summary>Gets the converter for a member discriminator.</summary>
	/// <exception cref="InvalidOperationException">Thrown for a type-level discriminator, which has no member converter.</exception>
	internal MessagePackConverter ValueConverter => this.converter ?? throw new InvalidOperationException();

	/// <summary>Creates the binding for an object, or <see langword="null"/> when the type declares none.</summary>
	/// <typeparam name="T">The object type.</typeparam>
	/// <param name="shape">The object shape.</param>
	/// <param name="owner">The converter cache.</param>
	/// <param name="getConverter">Resolves a converter for a discriminator value type.</param>
	/// <returns>The binding, or <see langword="null"/>.</returns>
	internal static UnionDiscriminatorBinding? Create<T>(IObjectTypeShape<T> shape, ConverterCache owner, Func<ITypeShape, MessagePackConverter> getConverter)
	{
		UnionDiscriminatorAttribute? typeLevel = FirstAttribute(shape.AttributeProvider);
		UnionDiscriminatorAttribute? memberLevel = null;
		IPropertyShape? member = null;
		int declarationIndex = -1;
		int memberDeclarationIndex = -1;
		bool anyKey = false;
		foreach (IPropertyShape property in shape.Properties)
		{
			if (property is IPropertyShape<T, UnusedDataPacket>)
			{
				continue;
			}

			declarationIndex++;
			if (!owner.IgnoreKeyAttributes && HasKey(property))
			{
				anyKey = true;
			}

			UnionDiscriminatorAttribute? attribute = FirstAttribute(property.AttributeProvider);
			if (attribute is null)
			{
				continue;
			}

			if (member is not null)
			{
				throw new NotSupportedException($"Type '{typeof(T).FullName}' declares more than one union discriminator.");
			}

			member = property;
			memberLevel = attribute;
			memberDeclarationIndex = declarationIndex;
		}

		if (typeLevel is not null && memberLevel is not null)
		{
			throw new NotSupportedException($"Type '{typeof(T).FullName}' declares both a type-level union discriminator and one on '{member!.Name}'.");
		}

		bool arrayLayout = shape.IsTupleType || owner.PerfOverSchemaStability || anyKey;
		if (member is not null)
		{
			UnionDiscriminatorBinding binding = MemberBinding<T>.Create(member, memberLevel!, owner, getConverter, shape);
			int? keyIndex = arrayLayout ? ArrayIndex(typeof(T), binding.Name, KeyOf(member, owner), memberDeclarationIndex, owner, shape.IsTupleType) : null;
			return binding.WithKeyIndex(keyIndex);
		}

		if (typeLevel is null)
		{
			return null;
		}

		if (arrayLayout)
		{
			throw new NotSupportedException($"Type '{typeof(T).FullName}' uses a keyed layout, so its union discriminator must be a member with {nameof(KeyAttribute)}.");
		}

		if (string.IsNullOrEmpty(typeLevel.PropertyName))
		{
			throw new NotSupportedException($"Type '{typeof(T).FullName}' declares a union discriminator without a property name.");
		}

		EnsureUniqueName(shape, typeLevel.PropertyName!, source: null, owner);
		return CreateTypeLevel(typeof(T), typeLevel, owner);
	}

	/// <summary>
	/// Requires cases that declare a discriminator to share one name, or one key index, and to use distinct values.
	/// </summary>
	/// <param name="caseShapes">The case shapes, including a concrete base when it is itself a case.</param>
	/// <param name="owner">The converter cache.</param>
	/// <param name="getConverter">Resolves a converter for a discriminator value type.</param>
	internal static void ValidateUnion(IEnumerable<ITypeShape> caseShapes, ConverterCache owner, Func<ITypeShape, MessagePackConverter> getConverter)
	{
		UnionDiscriminatorBinding? nameKeyed = null;
		UnionDiscriminatorBinding? keyed = null;
		HashSet<string> nameValues = new(StringComparer.Ordinal);
		HashSet<string> keyedValues = new(StringComparer.Ordinal);
		foreach (ITypeShape shape in caseShapes)
		{
			if (TryCreate(shape, owner, getConverter) is not UnionDiscriminatorBinding discriminator)
			{
				continue;
			}

			string token = discriminator.Token();
			if (discriminator.KeyIndex is null)
			{
				if (nameKeyed is null)
				{
					nameKeyed = discriminator;
				}
				else if (!string.Equals(nameKeyed.Name, discriminator.Name, StringComparison.Ordinal))
				{
					throw new NotSupportedException($"Union cases declare discriminator names '{nameKeyed.Name}' and '{discriminator.Name}'. Cases in one union must use the same discriminator name.");
				}

				if (!nameValues.Add(token))
				{
					throw new NotSupportedException($"The union discriminator '{discriminator.Name}' value is declared by more than one case.");
				}
			}
			else
			{
				if (keyed is null)
				{
					keyed = discriminator;
				}
				else if (keyed.KeyIndex != discriminator.KeyIndex)
				{
					throw new NotSupportedException($"Union cases declare discriminator indexes {keyed.KeyIndex} and {discriminator.KeyIndex}. Keyed cases in one union must use the same index.");
				}

				if (!keyedValues.Add(token))
				{
					throw new NotSupportedException($"The union discriminator at index {discriminator.KeyIndex} is declared with the same value by more than one case.");
				}
			}
		}
	}

	/// <summary>Writes the map key and the constant value.</summary>
	/// <param name="writer">The writer.</param>
	/// <param name="context">The serialization context.</param>
	internal void Write(ref MessagePackWriter writer, SerializationContext context)
	{
		writer.WriteRaw(this.msgpackName.Span);
		this.WriteValue(ref writer, context);
	}

	/// <summary>Writes the constant value.</summary>
	/// <param name="writer">The writer.</param>
	/// <param name="context">The serialization context.</param>
	internal void WriteValue(ref MessagePackWriter writer, SerializationContext context)
	{
		if (this.converter is not null)
		{
			this.converter.WriteObject(ref writer, this.expected, context);
			return;
		}

		if (this.enumByName && this.expected is Enum)
		{
			writer.Write(Enum.GetName(this.expected.GetType(), this.expected)!);
			return;
		}

		switch (this.expected)
		{
			case string text:
				writer.Write(text);
				break;
			case bool boolean:
				writer.Write(boolean);
				break;
			default:
				if (IsUnsigned(this.expected))
				{
					writer.Write(Convert.ToUInt64(this.expected, CultureInfo.InvariantCulture));
				}
				else
				{
					writer.Write(Convert.ToInt64(this.expected, CultureInfo.InvariantCulture));
				}

				break;
		}
	}

	/// <summary>Checks the value at the reader and does not assign it.</summary>
	/// <param name="reader">The reader positioned at the value.</param>
	/// <param name="context">The serialization context.</param>
	internal void Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (this.converter is not null)
		{
			object? actual;
			try
			{
				actual = this.converter.ReadObject(ref reader, context);
			}
			catch (Exception ex) when (IsPresentMismatch(ex))
			{
				throw Mismatch(this.Name, ex);
			}

			if (!Equals(actual, this.expected))
			{
				throw Mismatch(this.Name);
			}

			return;
		}

		try
		{
			if (!this.MatchesScalar(ref reader))
			{
				throw Mismatch(this.Name);
			}
		}
		catch (Exception ex) when (IsPresentMismatch(ex))
		{
			throw Mismatch(this.Name, ex);
		}
	}

	/// <summary>Reports whether <paramref name="utf8Name"/> is this discriminator's map key.</summary>
	/// <param name="utf8Name">The UTF-8 map key, without its MessagePack header.</param>
	/// <returns><see langword="true"/> when the name matches.</returns>
	internal bool Matches(ReadOnlySpan<byte> utf8Name) => utf8Name.SequenceEqual(this.utf8Name.Span);

	/// <summary>Creates the optional single-value schema for this discriminator.</summary>
	/// <param name="context">The schema context.</param>
	/// <returns>A schema with <c>const</c>, or a single-value <c>enum</c> in Draft 4.</returns>
	internal JsonObject CreateSchema(JsonSchemaContext context)
	{
		JsonObject schema = new() { ["type"] = this.schemaType };
		JsonNode node = CreateJsonNode(this.schemaValue);
		if (context.Dialect == JsonSchemaDialect.Draft4)
		{
			schema["enum"] = new JsonArray(node);
		}
		else
		{
			schema["const"] = node;
		}

		return schema;
	}

	private static UnionDiscriminatorBinding? TryCreate(ITypeShape shape, ConverterCache owner, Func<ITypeShape, MessagePackConverter> getConverter)
	{
		ITypeShape bare = shape is IUnionTypeShape union ? union.BaseType : shape;
		return bare.Accept(new ObjectFactory(owner, getConverter)) as UnionDiscriminatorBinding;
	}

	private static int? ArrayIndex(Type declaringType, string name, int? key, int declarationIndex, ConverterCache owner, bool tuple)
	{
		if (key is int keyed && !owner.IgnoreKeyAttributes)
		{
			return keyed;
		}

		if (owner.PerfOverSchemaStability || tuple)
		{
			return declarationIndex;
		}

		throw new NotSupportedException($"Type '{declaringType.FullName}' uses {nameof(KeyAttribute)}, so union discriminator '{name}' must have {nameof(KeyAttribute)} too.");
	}

	private static UnionDiscriminatorBinding CreateTypeLevel(Type declaringType, UnionDiscriminatorAttribute attribute, ConverterCache owner)
	{
		if (!attribute.HasValue || attribute.Value is null)
		{
			throw new NotSupportedException($"Type '{declaringType.FullName}' declares a union discriminator without a constant value.");
		}

		string name = attribute.PropertyName!;
		RejectReservedName(declaringType, name);
		object value = attribute.Value;
		if (value is string or bool)
		{
			return ForScalar(name, source: null, keyIndex: null, value, converter: null, enumByName: false);
		}

		if (value.GetType().IsEnum)
		{
			bool byName = owner.SerializeEnumValuesByName && Enum.GetName(value.GetType(), value) is not null;
			return ForScalar(name, source: null, keyIndex: null, value, converter: null, byName);
		}

		if (!IsInteger(value.GetType()))
		{
			throw new NotSupportedException($"Type '{declaringType.FullName}' declares a union discriminator of type '{value.GetType().FullName}', which is not a string, boolean, integer, or enum.");
		}

		return ForScalar(name, source: null, keyIndex: null, value, converter: null, enumByName: false);
	}

	private static UnionDiscriminatorBinding ForScalar(string name, IPropertyShape? source, int? keyIndex, object expected, MessagePackConverter? converter, bool enumByName)
		=> new(name, source, keyIndex, expected, converter, enumByName);

	private static (string Type, object Value) SchemaFromCaptured(byte[] valueBytes, object expected)
	{
		MessagePackReader reader = new(valueBytes);
		switch (reader.NextMessagePackType)
		{
			case MessagePackType.String:
				return ("string", reader.ReadString() ?? string.Empty);
			case MessagePackType.Boolean:
				return ("boolean", reader.ReadBoolean());
			case MessagePackType.Integer when IsUnsigned(expected):
				return ("integer", reader.ReadUInt64());
			case MessagePackType.Integer:
				return ("integer", reader.ReadInt64());
			default:
				throw new NotSupportedException($"Union discriminator value of type '{expected.GetType().FullName}' did not encode as a scalar.");
		}
	}

	private static JsonNode CreateJsonNode(object value) => value switch
	{
		string text => JsonValue.Create(text)!,
		bool boolean => JsonValue.Create(boolean)!,
		ulong unsigned => JsonValue.Create(unsigned)!,
		long signed => JsonValue.Create(signed)!,
		_ => JsonValue.Create(Convert.ToInt64(value, CultureInfo.InvariantCulture))!,
	};

	private static void EnsureUniqueName<T>(IObjectTypeShape<T> shape, string name, IPropertyShape? source, ConverterCache owner)
	{
		foreach (IPropertyShape property in shape.Properties)
		{
			if (property is IPropertyShape<T, UnusedDataPacket> || (source is not null && ReferenceEquals(property, source)))
			{
				continue;
			}

			string serialized = owner.GetSerializedPropertyName(property.Name, property.AttributeProvider);
			if (string.Equals(serialized, name, StringComparison.Ordinal))
			{
				throw new NotSupportedException($"Type '{typeof(T).FullName}' already has a property serialized as '{name}'.");
			}
		}
	}

	private static void RejectReservedName(Type declaringType, string name)
	{
		if (name is "$type" or "$value")
		{
			throw new NotSupportedException($"Type '{declaringType.FullName}' cannot use '{name}' as a union discriminator name.");
		}
	}

	private static UnionDiscriminatorAttribute? FirstAttribute(IGenericCustomAttributeProvider? provider)
	{
		if (provider is null)
		{
			return null;
		}

		foreach (UnionDiscriminatorAttribute attribute in provider.GetCustomAttributes<UnionDiscriminatorAttribute>(inherit: false))
		{
			return attribute;
		}

		return null;
	}

	private static bool HasKey(IPropertyShape property)
	{
		foreach (KeyAttribute _ in property.AttributeProvider.GetCustomAttributes<KeyAttribute>(inherit: false))
		{
			return true;
		}

		return false;
	}

	private static int? KeyOf(IPropertyShape property, ConverterCache owner)
	{
		if (owner.IgnoreKeyAttributes)
		{
			return null;
		}

		foreach (KeyAttribute key in property.AttributeProvider.GetCustomAttributes<KeyAttribute>(inherit: false))
		{
			return key.Index;
		}

		return null;
	}

	private static bool IsInteger(Type type)
		=> type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
			|| type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);

	private static bool IsUnsigned(object value)
		=> value is ulong || (value is Enum && Enum.GetUnderlyingType(value.GetType()) == typeof(ulong));

	private static bool IsPresentMismatch(Exception ex)
		=> ex is OverflowException || (ex is MessagePackSerializationException serialization && serialization.Code != MessagePackSerializationException.ErrorCode.DiscriminatorMismatch);

	private static MessagePackSerializationException Mismatch(string name, Exception? inner = null)
		=> new($"The union discriminator '{name}' does not match the expected value.", inner)
		{
			Code = MessagePackSerializationException.ErrorCode.DiscriminatorMismatch,
		};

	private UnionDiscriminatorBinding WithKeyIndex(int? keyIndex)
	{
		if (this.KeyIndex == keyIndex)
		{
			return this;
		}

		return new UnionDiscriminatorBinding(this.Name, this.Source, keyIndex, this.expected, this.converter, this.enumByName);
	}

	private string Token()
	{
#if NET
		return Convert.ToHexString(this.valueBytes);
#else
		return BitConverter.ToString(this.valueBytes);
#endif
	}

	private static byte[] CaptureValue(UnionDiscriminatorBinding binding)
	{
		using Sequence<byte> buffer = new();
		MessagePackWriter writer = new(buffer);
		if (binding.converter is IMessagePackConverterInternal preserved)
		{
			preserved.UnwrapReferencePreservation().WriteObject(ref writer, binding.expected, default);
		}
		else
		{
			binding.WriteValue(ref writer, default);
		}

		writer.Flush();
		return buffer.AsReadOnlySequence.ToArray();
	}

	private bool MatchesScalar(ref MessagePackReader reader)
	{
		if (this.expected is string text)
		{
			return string.Equals(reader.ReadString(), text, StringComparison.Ordinal);
		}

		if (this.expected is bool boolean)
		{
			return reader.ReadBoolean() == boolean;
		}

		if (this.expected is Enum && this.enumByName && reader.NextMessagePackType == MessagePackType.String)
		{
			return string.Equals(reader.ReadString(), Enum.GetName(this.expected.GetType(), this.expected), StringComparison.Ordinal);
		}

		if (IsUnsigned(this.expected))
		{
			return reader.ReadUInt64() == Convert.ToUInt64(this.expected, CultureInfo.InvariantCulture);
		}

		return reader.ReadInt64() == Convert.ToInt64(this.expected, CultureInfo.InvariantCulture);
	}

	private static class MemberBinding<TDeclaring>
	{
		internal static UnionDiscriminatorBinding Create(IPropertyShape property, UnionDiscriminatorAttribute attribute, ConverterCache owner, Func<ITypeShape, MessagePackConverter> getConverter, IObjectTypeShape<TDeclaring> shape)
			=> property.Accept(new Factory(attribute, owner, getConverter, shape)) as UnionDiscriminatorBinding ?? throw new NotSupportedException($"Union discriminator '{property.Name}' on '{typeof(TDeclaring).FullName}' must be an object property.");

		private sealed class Factory(UnionDiscriminatorAttribute attribute, ConverterCache owner, Func<ITypeShape, MessagePackConverter> getConverter, IObjectTypeShape<TDeclaring> shape) : TypeShapeVisitor
		{
			public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
				=> this.CreateProperty(propertyShape);

			public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null) => null;

			public override object? VisitConstructor<TDeclaringType, TArgumentState>(IConstructorShape<TDeclaringType, TArgumentState> constructorShape, object? state = null) => null;

			public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null) => null;

			public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null) => null;

			public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null) => null;

			public override object? VisitOptional<TOptional, TElement>(IOptionalTypeShape<TOptional, TElement> optionalShape, object? state = null) => null;

			public override object? VisitFunction<TFunction, TArgumentState, TResult>(IFunctionTypeShape<TFunction, TArgumentState, TResult> functionShape, object? state = null) => null;

			public override object? VisitSurrogate<T, TSurrogate>(ISurrogateTypeShape<T, TSurrogate> surrogateShape, object? state = null) => null;

			public override object? VisitUnion<TUnion>(IUnionTypeShape<TUnion> unionShape, object? state = null) => null;

			public override object? VisitUnionCase<TUnionCase, TUnion>(IUnionCaseShape<TUnionCase, TUnion> unionCaseShape, object? state = null) => null;

			public override object? VisitParameter<TArgumentState, TParameterType>(IParameterShape<TArgumentState, TParameterType> parameterShape, object? state = null) => null;

			private UnionDiscriminatorBinding CreateProperty<TDeclaringType, TProperty>(IPropertyShape<TDeclaringType, TProperty> property)
			{
				if (attribute.PropertyName is not null)
				{
					throw new NotSupportedException($"Union discriminator '{property.Name}' on '{typeof(TDeclaringType).FullName}' does not take a property name. Apply the name and value to the type, or the value to the member.");
				}

				if (!IsSupported(typeof(TProperty)))
				{
					throw new NotSupportedException($"Union discriminator '{property.Name}' on '{typeof(TDeclaringType).FullName}' has unsupported type '{typeof(TProperty).FullName}'.");
				}

				if (!property.HasGetter)
				{
					throw new NotSupportedException($"Union discriminator '{property.Name}' on '{typeof(TDeclaringType).FullName}' must be readable.");
				}

				if (property.HasSetter)
				{
					throw new NotSupportedException($"Union discriminator '{property.Name}' on '{typeof(TDeclaringType).FullName}' must not have a setter.");
				}

				if (shape.Constructor is { } constructor)
				{
					foreach (IParameterShape parameter in constructor.Parameters)
					{
						if (string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase))
						{
							throw new NotSupportedException($"Union discriminator '{property.Name}' on '{typeof(TDeclaringType).FullName}' must not be bound to a constructor parameter.");
						}
					}
				}

				string name = owner.GetSerializedPropertyName(property.Name, property.AttributeProvider);
				RejectReservedName(typeof(TDeclaringType), name);
				EnsureUniqueName(shape, name, property, owner);
				TProperty actual = ReadConstant(property);
				if (actual is null)
				{
					throw new NotSupportedException($"Union discriminator '{property.Name}' on '{typeof(TDeclaringType).FullName}' cannot be null.");
				}

				TProperty expected = attribute.HasValue ? ConvertValue<TProperty>(property.Name, attribute.Value) : actual;
				if (!EqualityComparer<TProperty>.Default.Equals(actual, expected))
				{
					throw new NotSupportedException($"Union discriminator '{property.Name}' on '{typeof(TDeclaringType).FullName}' returns '{actual}' instead of '{expected}'.");
				}

				MessagePackConverter converter = getConverter(property.PropertyType);
				MessagePackConverter bare = ((IMessagePackConverterInternal)converter).UnwrapReferencePreservation();
				bool byName = expected is Enum && bare.GetType().Name.StartsWith("EnumAsStringConverter", StringComparison.Ordinal);
				return ForScalar(name, property, keyIndex: null, expected, converter, byName);
			}

			private static bool IsSupported(Type type)
				=> type == typeof(string) || type == typeof(bool) || type.IsEnum || IsInteger(type);

			private static TProperty ReadConstant<TDeclaringType, TProperty>(IPropertyShape<TDeclaringType, TProperty> property)
			{
				try
				{
					TDeclaringType instance = CreateUninitialized<TDeclaringType>();
					return property.GetGetter()(ref instance);
				}
				catch (Exception ex) when (ex is not NotSupportedException)
				{
					throw new NotSupportedException($"Union discriminator '{property.Name}' on '{typeof(TDeclaringType).FullName}' must be a constant.", ex);
				}
			}

#if NET
			[UnconditionalSuppressMessage("Trimming", "IL2087", Justification = "The uninitialized instance is only used to read a constant getter.")]
#endif
			private static TDeclaringType CreateUninitialized<TDeclaringType>()
			{
				if (typeof(TDeclaringType).IsValueType)
				{
					return default!;
				}

#if NET
				TDeclaringType instance = (TDeclaringType)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TDeclaringType));
#else
				TDeclaringType instance = (TDeclaringType)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TDeclaringType));
#endif
				GC.SuppressFinalize(instance);
				return instance;
			}

			private static TProperty ConvertValue<TProperty>(string propertyName, object? value)
			{
				if (value is null)
				{
					throw new NotSupportedException($"Union discriminator '{propertyName}' on '{typeof(TDeclaring).FullName}' cannot be null.");
				}

				if (value is TProperty typed)
				{
					return typed;
				}

				if (typeof(TProperty).IsEnum && IsInteger(value.GetType()))
				{
					return (TProperty)Enum.ToObject(typeof(TProperty), value);
				}

				if (IsInteger(value.GetType()) && IsInteger(typeof(TProperty)))
				{
					try
					{
						return (TProperty)Convert.ChangeType(value, typeof(TProperty), CultureInfo.InvariantCulture);
					}
					catch (Exception ex) when (ex is OverflowException or InvalidCastException or FormatException)
					{
						throw new NotSupportedException($"Union discriminator '{propertyName}' on '{typeof(TDeclaring).FullName}' value '{value}' does not fit '{typeof(TProperty).FullName}'.", ex);
					}
				}

				throw new NotSupportedException($"Union discriminator '{propertyName}' on '{typeof(TDeclaring).FullName}' value '{value}' is not a '{typeof(TProperty).FullName}'.");
			}
		}
	}

	private sealed class ObjectFactory(ConverterCache owner, Func<ITypeShape, MessagePackConverter> getConverter) : TypeShapeVisitor
	{
		public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
			=> Create(objectShape, owner, getConverter);

		public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null) => null;

		public override object? VisitConstructor<TDeclaringType, TArgumentState>(IConstructorShape<TDeclaringType, TArgumentState> constructorShape, object? state = null) => null;

		public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null) => null;

		public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null) => null;

		public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null) => null;

		public override object? VisitOptional<TOptional, TElement>(IOptionalTypeShape<TOptional, TElement> optionalShape, object? state = null) => null;

		public override object? VisitFunction<TFunction, TArgumentState, TResult>(IFunctionTypeShape<TFunction, TArgumentState, TResult> functionShape, object? state = null) => null;

		public override object? VisitSurrogate<T, TSurrogate>(ISurrogateTypeShape<T, TSurrogate> surrogateShape, object? state = null) => null;

		public override object? VisitUnion<TUnion>(IUnionTypeShape<TUnion> unionShape, object? state = null) => null;

		public override object? VisitUnionCase<TUnionCase, TUnion>(IUnionCaseShape<TUnionCase, TUnion> unionCaseShape, object? state = null) => null;

		public override object? VisitParameter<TArgumentState, TParameterType>(IParameterShape<TArgumentState, TParameterType> parameterShape, object? state = null) => null;
	}
}
