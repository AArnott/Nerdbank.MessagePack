// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RS0026 // optional parameter on a method with overloads


namespace Nerdbank.PolySerializer.MessagePack;

/// <summary>
/// Serializes .NET objects using the MessagePack format.
/// </summary>
/// <devremarks>
/// <para>
/// This class may declare properties that customize how msgpack serialization is performed.
/// These properties must use <see langword="init"/> accessors to prevent modification after construction,
/// since there is no means to replace converters once they are created.
/// </para>
/// <para>
/// If the ability to add custom converters is exposed publicly, such a method should throw once generated converters have started being generated
/// because generated ones have already locked-in their dependencies.
/// </para>
/// </devremarks>
public partial record MessagePackSerializer : SerializerBase
{
	internal override ReusableObjectPool<IReferenceEqualityTracker>? ReferenceTrackingPool { get; } = new(() => new ReferenceEqualityTracker());

	public MessagePackSerializer()
		: base(new MsgPackConverterCache(MsgPackFormatter.Default, MsgPackDeformatter.Default))
	{
	}

	internal static MsgPackDeformatter MyDeformatter => MsgPackDeformatter.Default;

	internal new MsgPackConverterCache ConverterCache
	{
		get => (MsgPackConverterCache)base.ConverterCache;
		init => base.ConverterCache = value;
	}

	protected internal override Formatter Formatter => this.ConverterCache.Formatter;

	protected internal override Deformatter Deformatter => this.ConverterCache.Deformatter;

	internal static MsgPackStreamingDeformatter StreamingDeformatter => MsgPackStreamingDeformatter.Default;

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

	protected internal override void RenderAsJson(ref Reader reader, TextWriter writer)
	{
		switch (MessagePackCode.ToMessagePackType(reader.NextCode))
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
