// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Reads and writes a keyed union discriminator without assigning the property.
/// </summary>
/// <typeparam name="TDeclaringType">The type that declares the discriminator.</typeparam>
internal sealed class DiscriminatorPropertyAccessors<TDeclaringType> : PropertyAccessors<TDeclaringType>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DiscriminatorPropertyAccessors{TDeclaringType}"/> class.
	/// </summary>
	/// <param name="binding">The discriminator binding. Its source property must be set.</param>
	internal DiscriminatorPropertyAccessors(UnionDiscriminatorBinding binding)
		: base(CreateWriters(binding), CreateReaders(binding), binding.ValueConverter, shouldSerialize: null, binding.Source ?? throw new ArgumentException("A keyed discriminator requires a member.", nameof(binding)))
	{
		this.Binding = binding;
	}

	/// <summary>Gets the discriminator binding.</summary>
	internal UnionDiscriminatorBinding Binding { get; }

	/// <summary>Creates the schema for this discriminator.</summary>
	/// <param name="context">The schema context.</param>
	/// <returns>An optional single-value schema.</returns>
	internal JsonObject CreateSchema(JsonSchemaContext context) => this.Binding.CreateSchema(context);

	/// <summary>Reads and checks the discriminator value.</summary>
	/// <param name="reader">The reader positioned at the value.</param>
	/// <param name="context">The serialization context.</param>
	internal void Read(ref MessagePackReader reader, SerializationContext context) => this.Binding.Read(ref reader, context);

	private static (SerializeProperty<TDeclaringType> Serialize, SerializePropertyAsync<TDeclaringType> SerializeAsync) CreateWriters(UnionDiscriminatorBinding binding)
	{
		SerializeProperty<TDeclaringType> write = (in TDeclaringType value, ref MessagePackWriter writer, in SerializationContext context) =>
		{
			_ = value;
			binding.WriteValue(ref writer, context);
		};
		SerializePropertyAsync<TDeclaringType> writeAsync = (TDeclaringType value, MessagePackAsyncWriter writer, SerializationContext context) =>
		{
			_ = value;
			MessagePackWriter sync = writer.CreateWriter();
			binding.WriteValue(ref sync, context);
			writer.ReturnWriter(ref sync);
			return default;
		};
		return (write, writeAsync);
	}

	private static (DeserializeProperty<TDeclaringType> Deserialize, DeserializePropertyAsync<TDeclaringType> DeserializeAsync) CreateReaders(UnionDiscriminatorBinding binding)
	{
		DeserializeProperty<TDeclaringType> read = (ref TDeclaringType value, ref MessagePackReader reader, in SerializationContext context) =>
		{
			_ = value;
			binding.Read(ref reader, context);
		};
		DeserializePropertyAsync<TDeclaringType> readAsync = async (TDeclaringType value, MessagePackAsyncReader reader, SerializationContext context) =>
		{
			await reader.BufferNextStructuresAsync(1, 1, context).ConfigureAwait(false);
			MessagePackReader sync = reader.CreateBufferedReader();
			try
			{
				binding.Read(ref sync, context);
			}
			finally
			{
				reader.ReturnReader(ref sync);
			}

			return value;
		};
		return (read, readAsync);
	}
}
