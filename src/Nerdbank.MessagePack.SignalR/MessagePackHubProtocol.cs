// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;

namespace Nerdbank.MessagePack.SignalR;

/// <summary>
/// Implements the SignalR Hub Protocol using Nerdbank.MessagePack serialization.
/// </summary>
/// <remarks>
/// This implementation is designed to conform with <see href="https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/docs/specs/HubProtocol.md#messagepack-msgpack-encoding">this SignalR spec</see>
/// and more particularly to be compatible with <see href="https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/common/Protocols.MessagePack/src/Protocol/MessagePackHubProtocolWorker.cs">this implementation</see>.
/// </remarks>
internal partial class MessagePackHubProtocol : IHubProtocol
{
	private const int ErrorResult = 1;
	private const int VoidResult = 2;
	private const int NonVoidResult = 3;

	private static readonly MessagePackSerializer EnvelopeSerializer = new();

	private readonly MessagePackSerializer userSerializer;
	private readonly ITypeShapeProvider userTypeShapeProvider;

	/// <inheritdoc cref="MessagePackHubProtocol(ITypeShapeProvider, MessagePackSerializer)"/>
	public MessagePackHubProtocol(ITypeShapeProvider typeShapeProvider)
		: this(typeShapeProvider, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackHubProtocol"/> class.
	/// </summary>
	/// <param name="typeShapeProvider">The type shape provider for the parameter and return types used by invokable APIs.</param>
	/// <param name="serializer">The MessagePack serializer to use.</param>
	public MessagePackHubProtocol(ITypeShapeProvider typeShapeProvider, MessagePackSerializer? serializer = null)
	{
		this.userTypeShapeProvider = typeShapeProvider ?? throw new ArgumentNullException(nameof(typeShapeProvider));
		this.userSerializer = serializer ?? new();
	}

	/// <inheritdoc />
	public string Name => "messagepack";

	/// <inheritdoc />
	public int Version => 2;

	/// <inheritdoc />
	public TransferFormat TransferFormat => TransferFormat.Binary;

	/// <inheritdoc />
	public bool IsVersionSupported(int version) => version <= this.Version;

	/// <inheritdoc />
	public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out HubMessage? message)
	{
		if (!BinaryMessageFormatter.TryParseMessage(ref input, out ReadOnlySequence<byte> payload))
		{
			message = null;
			return false;
		}

		try
		{
			MessagePackReader reader = new(payload);
			message = this.ParseMessage(ref reader, binder);
			return message is not null;
		}
		catch (Exception ex) when (ex is not InvalidDataException)
		{
			throw new InvalidDataException("Invalid MessagePack data", ex);
		}
	}

	/// <inheritdoc />
	public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
	{
		Requires.NotNull(message);
		Requires.NotNull(output);

		using SequencePool<byte>.Rental sequenceRental = SequencePool<byte>.Shared.Rent();

		try
		{
			// Write message to a buffer so we can get its length
			this.WriteMessageCore(sequenceRental.Value, message);

			// Write length then message to output
			BinaryMessageFormatter.WriteLengthPrefix(sequenceRental.Value.Length, output);
			output.Write(sequenceRental.Value);
		}
		catch (Exception ex) when (ex is not InvalidDataException)
		{
			throw new InvalidDataException("Failed to serialize the message.", ex);
		}
	}

	/// <inheritdoc />
	public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
	{
		Requires.NotNull(message);

		using SequencePool<byte>.Rental sequenceRental = SequencePool<byte>.Shared.Rent();

		try
		{
			// Write message to a buffer so we can get its length
			this.WriteMessageCore(sequenceRental.Value, message);

			// Write length then message to output
			int prefixLength = BinaryMessageFormatter.LengthPrefixLength(sequenceRental.Value.Length);

			byte[] array = new byte[sequenceRental.Value.Length + prefixLength];
			Span<byte> span = array.AsSpan();

			// Write length then message to output
			int written = BinaryMessageFormatter.WriteLengthPrefix(sequenceRental.Value.Length, span);
			Debug.Assert(written == prefixLength, "LengthPrefixLength lied to us.");
			sequenceRental.Value.AsReadOnlySequence.CopyTo(span.Slice(prefixLength));

			return array;
		}
		catch (Exception ex) when (ex is not InvalidDataException)
		{
			throw new InvalidDataException("Failed to serialize the message.", ex);
		}
	}

	[GenerateShapeFor<IDictionary<string, string>>]
	[GenerateShapeFor<string[]>]
	private partial class Witness;
}
