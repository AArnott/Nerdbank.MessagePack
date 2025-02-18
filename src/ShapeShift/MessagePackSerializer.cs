// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

using ShapeShift.MessagePack;

namespace ShapeShift;

/// <summary>
/// Serializes .NET objects using the MessagePack format.
/// </summary>
public record MessagePackSerializer : SerializerBase
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackSerializer"/> class.
	/// </summary>
	public MessagePackSerializer()
		: base(new MsgPackConverterCache(MsgPackFormatter.Default, MsgPackDeformatter.Default))
	{
	}

	/// <inheritdoc cref="ConverterCache.PreserveReferences"/>
	public bool PreserveReferences
	{
		get => this.ConverterCache.PreserveReferences;
		init => this.ConverterCache = this.ConverterCache with { PreserveReferences = value };
	}

	/// <summary>
	/// Gets the extension type codes to use for library-reserved extension types.
	/// </summary>
	/// <remarks>
	/// This property may be used to reassign the extension type codes for library-provided extension types
	/// in order to avoid conflicts with other libraries the application is using.
	/// </remarks>
	public LibraryReservedMessagePackExtensionTypeCode LibraryExtensionTypeCodes { get; init; } = LibraryReservedMessagePackExtensionTypeCode.Default;

	/// <summary>
	/// Gets a value indicating whether hardware accelerated converters should be avoided.
	/// </summary>
	internal bool DisableHardwareAcceleration
	{
		get => this.ConverterCache.DisableHardwareAcceleration;
		init => this.ConverterCache = this.ConverterCache with { DisableHardwareAcceleration = value };
	}

	/// <inheritdoc cref="SerializerBase.ConverterCache" />
	internal new MsgPackConverterCache ConverterCache
	{
		get => (MsgPackConverterCache)base.ConverterCache;
		init => base.ConverterCache = value;
	}

	/// <inheritdoc />
	internal override ReusableObjectPool<IReferenceEqualityTracker>? ReferenceTrackingPool { get; } = new(() => new ReferenceEqualityTracker());

	/// <inheritdoc />
	protected internal override void RenderAsJson(ref Reader reader, TextWriter writer)
	{
		Requires.NotNull(writer);

		switch (((MsgPackDeformatter)reader.Deformatter).PeekNextMessagePackType(reader))
		{
			case MessagePackType.Extension:
				Extension extension = ((MsgPackDeformatter)this.Deformatter).ReadExtension(ref reader);
				writer.Write($"\"msgpack extension {extension.Header.TypeCode} as base64: ");
				writer.Write(Convert.ToBase64String(extension.Data.ToArray()));
				writer.Write('\"');
				break;
			case MessagePackType type:
				throw new NotImplementedException($"{type} not yet implemented.");
		}
	}
}
