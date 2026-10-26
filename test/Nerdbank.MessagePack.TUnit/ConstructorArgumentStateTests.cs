// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType.SourceGenModel;

public partial class ConstructorArgumentStateTests : MessagePackSerializerTestBase
{
	public enum ReadOutcome
	{
		Success,
		InvalidData,
		MissingRequiredArgument,
		ThrowingConstructor,
		Cancellation,
	}

	[Test, MatrixDataSource]
	public async Task Read_ReturnsArgumentState(bool async, bool indexed, ReadOutcome outcome)
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = indexed };
		TrackingArgumentState state = new() { AreRequiredArgumentsSet = outcome != ReadOutcome.MissingRequiredArgument };
		InvalidOperationException constructorException = new("Constructor failed.");
		using CancellationTokenSource cancellationSource = new();
		IObjectTypeShape<Model> originalShape = (IObjectTypeShape<Model>)TypeShapeResolver.ResolveDynamicOrThrow<Model>();
		SourceGenObjectTypeShape<Model> shape = new()
		{
			Provider = originalShape.Provider,
			IsContextual = true,
			PropertiesFactory = () => originalShape.Properties,
			ConstructorFactory = () => new SourceGenConstructorShape<Model, TrackingArgumentState>
			{
				DeclaringType = originalShape,
				IsPublic = true,
				ArgumentStateConstructor = () => state,
				ParameterizedConstructor = Construct,
				ParametersFactory = () =>
				[
					new SourceGenParameterShape<TrackingArgumentState, int>
					{
						Position = 0,
						Name = nameof(Model.Value),
						Kind = ParameterKind.MethodParameter,
						IsRequired = true,
						IsPublic = true,
						ParameterType = ((IPropertyShape<Model, int>)originalShape.Properties[0]).PropertyType,
						Getter = (ref TrackingArgumentState argumentState) => argumentState.Value,
						Setter = (ref TrackingArgumentState argumentState, int value) => argumentState.Value = value,
					},
				],
			},
		};
		byte[] data = [outcome == ReadOutcome.InvalidData ? MessagePackCode.NeverUsed : indexed ? MessagePackCode.MinFixArray : MessagePackCode.MinFixMap];

		switch (outcome)
		{
			case ReadOutcome.Success:
				Assert.NotNull(await Read());
				break;
			case ReadOutcome.InvalidData:
				await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await Read());
				break;
			case ReadOutcome.MissingRequiredArgument:
				MessagePackSerializationException ex = await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await Read());
				Assert.Equal(MessagePackSerializationException.ErrorCode.MissingRequiredProperty, Assert.IsType<MessagePackSerializationException>(ex.GetBaseException()).Code);
				break;
			case ReadOutcome.ThrowingConstructor:
				MessagePackSerializationException ctorEx = await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await Read());
				Assert.Same(constructorException, ctorEx.GetBaseException());
				break;
			case ReadOutcome.Cancellation:
				await Assert.ThrowsAsync<OperationCanceledException>(async () => await Read());
				break;
		}

		Assert.Equal(1, state.ReturnCount);

		Model Construct(ref TrackingArgumentState argumentState)
		{
			Assert.Same(state, argumentState);
			Assert.Equal(0, state.ReturnCount);
			if (outcome == ReadOutcome.ThrowingConstructor)
			{
				throw constructorException;
			}

			if (outcome == ReadOutcome.Cancellation)
			{
				cancellationSource.Cancel();
				cancellationSource.Token.ThrowIfCancellationRequested();
			}

			return new(argumentState.Value);
		}

		async ValueTask<Model?> Read()
		{
			if (async)
			{
				using MemoryStream stream = new(data);
				return await this.Serializer.DeserializeAsync(stream, shape, cancellationSource.Token);
			}
			else
			{
				return this.Serializer.Deserialize(new ReadOnlySequence<byte>(data), shape, cancellationSource.Token);
			}
		}
	}

	[GenerateShape]
	public partial record Model(int Value);

#pragma warning disable PT0019 // Test double for verifying the argument-state lifetime contract.
	private class TrackingArgumentState : IArgumentState
#pragma warning restore PT0019
	{
		public int Count => 1;

		public bool AreRequiredArgumentsSet { get; init; }

		public int Value { get; set; }

		public int ReturnCount { get; private set; }

		public bool IsArgumentSet(int index) => this.AreRequiredArgumentsSet;

		public void Return() => this.ReturnCount++;
	}
}
