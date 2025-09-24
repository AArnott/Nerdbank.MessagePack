// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack;

/// <summary>
/// The result of an operation that was to produce a <see cref="MessagePackConverter"/>.
/// </summary>
internal interface IConverterResult
{
	/// <summary>
	/// Gets a value indicating whether the operation was successful.
	/// </summary>
	[MemberNotNullWhen(true, nameof(Value))]
	[MemberNotNullWhen(false, nameof(Error))]
	bool Success { get; }

	/// <summary>
	/// Gets the converter.
	/// </summary>
	/// <exception cref="MessagePackSerializationException">Thrown if <see cref="Success"/> is <see langword="false"/>.</exception>
	MessagePackConverter? Value { get; }

	/// <summary>
	/// Gets the converter, or throws if the result is a failure.
	/// </summary>
	MessagePackConverter ValueOrThrow { get; }

	/// <summary>
	/// Gets the error that describes why the operation failed.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Success"/> is <see langword="true"/>.</exception>
	VisitorError? Error { get; }

	/// <summary>
	/// Wraps the converter with one that provides reference preservation capabilities.
	/// </summary>
	/// <returns>A new result with the wrapped converter, or this same instance if it is already a failure.</returns>
	IConverterResult WrapWithReferencePreservation();

	/// <summary>
	/// Prepares a more descriptive failure result, if this result is a failure.
	/// </summary>
	/// <typeparam name="T">The type argument for the returned <see cref="ConverterResult{T}"/>.</typeparam>
	/// <param name="stepMessage">The message to prepend to the error path.</param>
	/// <param name="failureResult">Receives the new failure result, if this one is a failure.</param>
	/// <returns><see langword="true"/> if this is a failure and <paramref name="failureResult"/> is set; <see langword="false"/> otherwise.</returns>
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	bool TryPrepareFailPath<T>(string stepMessage, [NotNullWhen(true)] out ConverterResult<T>? failureResult);

	/// <summary>
	/// Prepares a more descriptive failure result, if this result is a failure.
	/// </summary>
	/// <typeparam name="T">The type argument for the returned <see cref="ConverterResult{T}"/>.</typeparam>
	/// <param name="target">The shape that failed to produce a converter.</param>
	/// <param name="failureResult">Receives the new failure result, if this one is a failure.</param>
	/// <returns><see langword="true"/> if this is a failure and <paramref name="failureResult"/> is set; <see langword="false"/> otherwise.</returns>
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	bool TryPrepareFailPath<T>(ITypeShape target, [NotNullWhen(true)] out ConverterResult<T>? failureResult);

	/// <inheritdoc cref="TryPrepareFailPath{T}(ITypeShape, out ConverterResult{T}?)" />
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	bool TryPrepareFailPath<T>(IPropertyShape target, [NotNullWhen(true)] out ConverterResult<T>? failureResult);

	/// <inheritdoc cref="TryPrepareFailPath{T}(ITypeShape, out ConverterResult{T}?)" />
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	bool TryPrepareFailPath<T>(IParameterShape target, [NotNullWhen(true)] out ConverterResult<T>? failureResult);
}

/// <summary>
/// A factory for <see cref="ConverterResult{T}"/> instances.
/// </summary>
internal static class ConverterResult
{
	/// <summary>
	/// Creates a successful result.
	/// </summary>
	/// <typeparam name="T">The type of value the converter handles.</typeparam>
	/// <param name="value">The converter.</param>
	/// <returns>A successful result.</returns>
	internal static ConverterResult<T> Ok<T>(MessagePackConverter<T> value) => new(value);

	/// <summary>
	/// Creates a failure result.
	/// </summary>
	/// <typeparam name="T">The type of value the converter would have handled.</typeparam>
	/// <param name="error">The error describing the failure.</param>
	/// <returns>A failure result.</returns>
	internal static ConverterResult<T> Err<T>(VisitorError error) => new(error);

	/// <summary>
	/// Maps a <see cref="Result{TValue, TError}"/> to a <see cref="ConverterResult{T}"/> using the specified <paramref name="mapper"/>.
	/// </summary>
	/// <typeparam name="T1">The type of result of the original value.</typeparam>
	/// <typeparam name="T2">The type of result coming from the <paramref name="mapper"/>.</typeparam>
	/// <param name="result">The original result.</param>
	/// <param name="mapper">The mapper.</param>
	/// <returns>The final result.</returns>
	internal static ConverterResult<T2> MapResult<T1, T2>(this Result<T1, VisitorError> result, Func<T1, MessagePackConverter<T2>> mapper)
	{
		return result.Success ? Ok(mapper(result.Value)) : Err<T2>(result.Error);
	}
}

/// <summary>
/// The result of an operation that was to produce a <see cref="MessagePackConverter{T}"/>.
/// </summary>
/// <typeparam name="T">The type of value the converter can serialize.</typeparam>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
internal class ConverterResult<T> : IConverterResult
{
	private readonly MessagePackConverter<T>? value;
	private readonly VisitorError? error;

	/// <summary>
	/// Initializes a new instance of the <see cref="ConverterResult{T}"/> class that represents success.
	/// </summary>
	/// <param name="value">The converter.</param>
	internal ConverterResult(MessagePackConverter<T> value)
	{
		this.value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ConverterResult{T}"/> class that represents failure.
	/// </summary>
	/// <param name="error">The error.</param>
	internal ConverterResult(VisitorError error)
	{
		this.error = error;
	}

	/// <inheritdoc />
	[MemberNotNullWhen(true, nameof(Value))]
	[MemberNotNullWhen(false, nameof(Error))]
	public bool Success => this.value is not null;

	/// <summary>
	/// Gets the converter.
	/// </summary>
	/// <exception cref="MessagePackSerializationException">Thrown if <see cref="Success"/> is <see langword="false"/>.</exception>
	public MessagePackConverter<T>? Value => this.value;

	/// <summary>
	/// Gets the converter, or throws if the result is a failure.
	/// </summary>
	public MessagePackConverter<T> ValueOrThrow
	{
		get
		{
			this.error?.ThrowException();
			return this.value!;
		}
	}

	/// <inheritdoc/>
	MessagePackConverter IConverterResult.ValueOrThrow => this.ValueOrThrow;

	/// <inheritdoc />
	public VisitorError? Error => this.error;

	/// <inheritdoc />
	MessagePackConverter? IConverterResult.Value => this.Value;

	/// <summary>
	/// Gets a  string used by the debugger to display the current result.
	/// </summary>
	private string DebuggerDisplay => this.Success ? $"Ok: {this.value}" : $"Err: {this.error}";

	/// <summary>
	/// Creates a successful result.
	/// </summary>
	/// <param name="value">The converter.</param>
	/// <returns>A successful result.</returns>
	public static ConverterResult<T> Ok(MessagePackConverter<T> value) => new(value);

	/// <summary>
	/// Creates a failure result.
	/// </summary>
	/// <param name="error">The error describing the failure.</param>
	/// <returns>A failure result.</returns>
	public static ConverterResult<T> Err(VisitorError error) => new(error);

	/// <inheritdoc cref="IConverterResult.TryPrepareFailPath{T}(string, out ConverterResult{T}?)"/>
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool TryPrepareFailPath(string stepMessage, [NotNullWhen(true)] out ConverterResult<T>? failureResult)
		=> this.TryPrepareFailPath<T>(stepMessage, out failureResult);

	/// <inheritdoc cref="TryPrepareFailPath{T}(ITypeShape, out ConverterResult{T}?)" />
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool TryPrepareFailPath(IPropertyShape property, [NotNullWhen(true)] out ConverterResult<T>? failureResult)
		=> this.TryPrepareFailPath<T>(property, out failureResult);

	/// <inheritdoc cref="IConverterResult.TryPrepareFailPath{T}(ITypeShape, out ConverterResult{T}?)"/>
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool TryPrepareFailPath(ITypeShape typeShape, [NotNullWhen(true)] out ConverterResult<T>? failureResult)
		=> this.TryPrepareFailPath<T>(typeShape, out failureResult);

	/// <inheritdoc />
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool TryPrepareFailPath<T1>(string stepMessage, [NotNullWhen(true)] out ConverterResult<T1>? failureResult)
	{
		if (this.Success)
		{
			failureResult = null;
			return false;
		}

		failureResult = new ConverterResult<T1>(new VisitorError(stepMessage, this.Error));
		return true;
	}

	/// <inheritdoc />
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool TryPrepareFailPath<T1>(IPropertyShape shape, [NotNullWhen(true)] out ConverterResult<T1>? failureResult)
	{
		if (this.Success)
		{
			failureResult = null;
			return false;
		}

		failureResult = ConverterResult.Err<T1>(new VisitorError($"{shape.DeclaringType.Type.FullName}.{shape.Name}", this.Error));
		return true;
	}

	/// <inheritdoc />
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool TryPrepareFailPath<T1>(IParameterShape shape, [NotNullWhen(true)] out ConverterResult<T1>? failureResult)
	{
		if (this.Success)
		{
			failureResult = null;
			return false;
		}

		failureResult = ConverterResult.Err<T1>(new VisitorError($"{shape.Name}", this.Error));
		return true;
	}

	/// <inheritdoc cref="TryPrepareFailPath{T}(ITypeShape, out ConverterResult{T}?)" />
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool TryPrepareFailPath<T1>(IUnionCaseShape target, [NotNullWhen(true)] out ConverterResult<T1>? failureResult)
	{
		if (this.Success)
		{
			failureResult = null;
			return false;
		}

		failureResult = ConverterResult.Err<T1>(new VisitorError($"Union case: {target.UnionCaseType.Type.FullName ?? target.UnionCaseType.Type.Name}", this.Error));
		return true;
	}

	/// <inheritdoc cref="TryPrepareFailPath{T}(ITypeShape, out ConverterResult{T}?)" />
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool TryPrepareFailPath<T1>(IOptionalTypeShape shape, [NotNullWhen(true)] out ConverterResult<T1>? failureResult)
	{
		if (this.Success)
		{
			failureResult = null;
			return false;
		}

		failureResult = ConverterResult.Err<T1>(new VisitorError($"Optional: {shape.Type.FullName ?? shape.Type.Name}", this.Error));
		return true;
	}

	/// <inheritdoc/>
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool TryPrepareFailPath<T1>(ITypeShape shape, [NotNullWhen(true)] out ConverterResult<T1>? failureResult)
	{
		if (this.Success)
		{
			failureResult = null;
			return false;
		}

		failureResult = ConverterResult.Err<T1>(new VisitorError(shape.Type.FullName ?? shape.Type.Name, this.Error));
		return true;
	}

	/// <inheritdoc cref="TryPrepareFailPath{T}(ITypeShape, out ConverterResult{T}?)" />
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool TryPrepareFailPath<T1>(ISurrogateTypeShape shape, [NotNullWhen(true)] out ConverterResult<T1>? failureResult)
	{
		if (this.Success)
		{
			failureResult = null;
			return false;
		}

		failureResult = ConverterResult.Err<T1>(new VisitorError($"surrogate {shape.SurrogateType.Type.FullName ?? shape.SurrogateType.Type.Name}", this.Error));
		return true;
	}

	/// <summary>
	/// Wraps the converter with one that provides reference preservation capabilities.
	/// </summary>
	/// <returns>A new result with the wrapped converter, or this same instance if it is already a failure.</returns>
	public ConverterResult<T> WrapWithReferencePreservation()
	{
		return this.Success ? Ok(this.value is ReferencePreservingConverter<T> already ? already : new ReferencePreservingConverter<T>(this.Value)) : this;
	}

	/// <inheritdoc />
	IConverterResult IConverterResult.WrapWithReferencePreservation() => this.WrapWithReferencePreservation();
}
