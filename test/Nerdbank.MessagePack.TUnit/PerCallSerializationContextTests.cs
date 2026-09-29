// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// Tests for the (de)serialize overloads that accept a per-call <see cref="SerializationContext"/>.
/// </summary>
public partial class PerCallSerializationContextTests : MessagePackSerializerTestBase
{
	private const string MultiplierKey = "PerCallSerializationContextTests.Multiplier";

	private static readonly ITypeShape<Scaled> ScaledShape = TypeShapeResolver.ResolveDynamicOrThrow<Scaled, Witness>();
	private static readonly ITypeShape<Outer> OuterShape = TypeShapeResolver.ResolveDynamicOrThrow<Outer, Witness>();

	[Test]
	public void Serialize_Deserialize_Generic()
	{
		SerializationContext context = this.CreateContext(multiplier: 3);

		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		this.Serializer.Serialize(ref writer, new Scaled(5), ScaledShape, context);
		writer.Flush();

		AssertEncodedInt(sequence, 15);

		MessagePackReader reader = new(sequence);
		Assert.Equal(new Scaled(5), this.Serializer.Deserialize(ref reader, ScaledShape, context));
	}

	[Test]
	public void SerializeObject_DeserializeObject()
	{
		SerializationContext context = this.CreateContext(multiplier: 3);

		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		this.Serializer.SerializeObject(ref writer, new Scaled(5), ScaledShape, context);
		writer.Flush();

		AssertEncodedInt(sequence, 15);

		MessagePackReader reader = new(sequence);
		Assert.Equal(new Scaled(5), this.Serializer.DeserializeObject(ref reader, ScaledShape, context));
	}

	[Test]
	public async Task SerializeAsync_DeserializeAsync_Generic()
	{
		SerializationContext context = this.CreateContext(multiplier: 3);

		Pipe pipe = new();
		await this.Serializer.SerializeAsync(pipe.Writer, new Scaled(5), ScaledShape, context);
		await pipe.Writer.FlushAsync(this.TimeoutToken);
		await pipe.Writer.CompleteAsync();

		Assert.Equal(new Scaled(5), await this.Serializer.DeserializeAsync(pipe.Reader, ScaledShape, context));
	}

	[Test]
	public async Task SerializeObjectAsync_DeserializeObjectAsync()
	{
		SerializationContext context = this.CreateContext(multiplier: 3);

		Pipe pipe = new();
		await this.Serializer.SerializeObjectAsync(pipe.Writer, new Scaled(5), ScaledShape, context);
		await pipe.Writer.FlushAsync(this.TimeoutToken);
		await pipe.Writer.CompleteAsync();

		Assert.Equal(new Scaled(5), await this.Serializer.DeserializeObjectAsync(pipe.Reader, ScaledShape, context));
	}

	[Test]
	public async Task AsyncOverloads_WrittenBytesReflectContext()
	{
		Pipe pipe = new();
		await this.Serializer.SerializeAsync(pipe.Writer, new Scaled(5), ScaledShape, this.CreateContext(multiplier: 7));
		await pipe.Writer.FlushAsync(this.TimeoutToken);
		await pipe.Writer.CompleteAsync();

		ReadResult result = await pipe.Reader.ReadAsync(this.TimeoutToken);
		AssertEncodedInt(result.Buffer, 35);
		pipe.Reader.AdvanceTo(result.Buffer.End);
	}

	[Test]
	public void StartingContextIsIgnoredWhenContextIsSupplied()
	{
		SerializationContext serializerContext = this.Serializer.StartingContext;
		serializerContext[MultiplierKey] = 2;
		this.Serializer = this.Serializer with { StartingContext = serializerContext };

		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		this.Serializer.Serialize(ref writer, new Scaled(5), ScaledShape, this.CreateContext(multiplier: 10));
		writer.Flush();
		AssertEncodedInt(sequence, 50);

		// The CancellationToken overload continues to use StartingContext.
		sequence.Reset();
		writer = new(sequence);
		this.Serializer.Serialize(ref writer, new Scaled(5), ScaledShape, this.TimeoutToken);
		writer.Flush();
		AssertEncodedInt(sequence, 10);
	}

	[Test]
	public void DefaultLiteralBindsToCancellationTokenOverload()
	{
		SerializationContext serializerContext = this.Serializer.StartingContext;
		serializerContext[MultiplierKey] = 4;
		this.Serializer = this.Serializer with { StartingContext = serializerContext };

		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);

		// This must compile without ambiguity, and use StartingContext (evident by the multiplier being applied).
		this.Serializer.Serialize(ref writer, new Scaled(5), ScaledShape, default);
		writer.Flush();
		AssertEncodedInt(sequence, 20);

		MessagePackReader reader = new(sequence);
		Assert.Equal(new Scaled(5), this.Serializer.Deserialize(ref reader, ScaledShape, default));
	}

	[Test]
	public void CancellationTokenComesFromContext()
	{
		CancellationTokenSource cts = new();
		cts.Cancel();
		SerializationContext context = this.CreateContext(multiplier: 1) with { CancellationToken = cts.Token };

		Sequence<byte> sequence = new();
		Assert.Throws<OperationCanceledException>(() =>
		{
			MessagePackWriter writer = new(sequence);
			this.Serializer.Serialize(ref writer, new Scaled(5), ScaledShape, context);
		});

		Assert.Throws<OperationCanceledException>(() =>
		{
			MessagePackReader reader = new(new byte[] { 0x05 });
			this.Serializer.Deserialize(ref reader, ScaledShape, context);
		});
	}

	[Test]
	public async Task CancellationTokenComesFromContext_Async()
	{
		CancellationTokenSource cts = new();
		cts.Cancel();
		SerializationContext context = this.CreateContext(multiplier: 1) with { CancellationToken = cts.Token };

		Pipe pipe = new();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await this.Serializer.SerializeAsync(pipe.Writer, new Scaled(5), ScaledShape, context));
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await this.Serializer.DeserializeAsync(pipe.Reader, ScaledShape, context));
	}

	[Test]
	public void MaxDepthComesFromContext()
	{
		Outer value = new(new Inner(3));

		// The serializer's own StartingContext permits this depth.
		this.Roundtrip(value, OuterShape);

		SerializationContext shallow = this.Serializer.StartingContext with { MaxDepth = 1 };
		Sequence<byte> sequence = new();
		Assert.Throws<MessagePackSerializationException>(() =>
		{
			MessagePackWriter writer = new(sequence);
			this.Serializer.Serialize(ref writer, value, OuterShape, shallow);
		});
	}

	[Test]
	public void InitializedContextIsRejected()
	{
		SerializationContext captured = this.CaptureInFlightContext();

		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
		{
			Sequence<byte> sequence = new();
			MessagePackWriter writer = new(sequence);
			this.Serializer.Serialize(ref writer, new Scaled(5), ScaledShape, captured);
		});
		Assert.Equal("startingContext", ex.ParamName);
		this.Logger.LogInformation(ex.Message);

		Assert.Throws<ArgumentException>(() =>
		{
			MessagePackReader reader = new(new byte[] { 0x05 });
			this.Serializer.DeserializeObject(ref reader, ScaledShape, captured);
		});

		// Async overloads reject the context synchronously.
		Pipe pipe = new();
		Assert.Throws<ArgumentException>(() => this.Serializer.SerializeAsync(pipe.Writer, new Scaled(5), ScaledShape, captured));
		Assert.Throws<ArgumentException>(() => this.Serializer.DeserializeObjectAsync(pipe.Reader, ScaledShape, captured));
	}

	[Test]
	public void StartingContextWithCapturedContextIsStillAccepted()
	{
		// Preserve prior behavior: an already-initialized context assigned to StartingContext was never validated.
		SerializationContext captured = this.CaptureInFlightContext();
		this.Serializer = this.Serializer with { StartingContext = captured };

		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		this.Serializer.Serialize(ref writer, new Scaled(5), ScaledShape, this.TimeoutToken);
		writer.Flush();
		AssertEncodedInt(sequence, 5);
	}

	[Test]
	public void ProtectedCreateSerializationContext()
	{
		DerivedSerializer serializer = new();
		Assert.Equal(6, serializer.GetMultiplierFromCreatedContext(this.CreateContext(multiplier: 6)));
		Assert.Throws<ArgumentException>(() => serializer.GetMultiplierFromCreatedContext(this.CaptureInFlightContext()));
	}

	private static void AssertEncodedInt(ReadOnlySequence<byte> msgpack, int expected)
	{
		MessagePackReader reader = new(msgpack);
		Assert.Equal(expected, reader.ReadInt32());
	}

	private SerializationContext CreateContext(int multiplier)
	{
		SerializationContext context = this.Serializer.StartingContext with { CancellationToken = this.TimeoutToken };
		context[MultiplierKey] = multiplier;
		return context;
	}

	private SerializationContext CaptureInFlightContext()
	{
		ContextCapture capture = new();
		SerializationContext context = this.CreateContext(multiplier: 1);
		context[typeof(ContextCapture)] = capture;

		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		this.Serializer.Serialize(ref writer, new Scaled(5), ScaledShape, context);
		return capture.Context ?? throw new InvalidOperationException("The converter did not capture its context.");
	}

	[MessagePackConverter(typeof(ScaledConverter))]
	internal partial record struct Scaled(int Value);

	[GenerateShapeFor<Scaled>]
	[GenerateShapeFor<Outer>]
	internal partial class Witness;

	internal partial record Outer(Inner? Inner);

	internal partial record Inner(int Value);

	internal class ContextCapture
	{
		internal SerializationContext? Context { get; set; }
	}

	internal class ScaledConverter : MessagePackConverter<Scaled>
	{
		public override Scaled Read(ref MessagePackReader reader, SerializationContext context)
			=> new(reader.ReadInt32() / GetMultiplier(context));

		public override void Write(ref MessagePackWriter writer, in Scaled value, SerializationContext context)
		{
			if (context[typeof(ContextCapture)] is ContextCapture capture)
			{
				capture.Context = context;
			}

			writer.Write(value.Value * GetMultiplier(context));
		}

		private static int GetMultiplier(SerializationContext context) => context[MultiplierKey] is int multiplier ? multiplier : 1;
	}

	private record DerivedSerializer : MessagePackSerializer
	{
		internal int GetMultiplierFromCreatedContext(SerializationContext startingContext)
		{
			using DisposableSerializationContext context = this.CreateSerializationContext(Witness.GeneratedTypeShapeProvider, startingContext);
			Assert.NotNull(context.Value.TypeShapeProvider);
			return (int)context.Value[MultiplierKey]!;
		}
	}
}
