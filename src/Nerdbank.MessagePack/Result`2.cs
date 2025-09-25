// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MessagePack;

/// <summary>
/// Represents the outcome of an operation that may have produced either a value of type <typeparamref name="TValue"/>
/// or an exception describing a failure.
/// </summary>
/// <typeparam name="TValue">The type of the successful result value.</typeparam>
/// <typeparam name="TError">The type of the error value.</typeparam>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
internal struct Result<TValue, TError> : IEquatable<Result<TValue, TError>>
{
	private bool? success;
	private TValue? value;
	private TError? error;

	/// <summary>
	/// Initializes a new instance of the <see cref="Result{TValue, TError}"/> struct representing a successful value.
	/// </summary>
	/// <param name="value">The successful result value.</param>
	private Result(TValue value)
	{
		this.value = value;
		this.success = true;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Result{TValue, TError}"/> struct representing a failure.
	/// </summary>
	/// <param name="error">The exception that represents the failure.</param>
	private Result(TError error)
	{
		this.error = error;
		this.success = false;
	}

	/// <summary>
	/// Gets a value indicating whether this instance is the default, i.e. no result (neither success nor failure) was recorded.
	/// </summary>
	public bool IsDefault => this.success is null;

	/// <summary>
	/// Gets a value indicating whether the recorded result represents a success.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="IsDefault"/> is <see langword="true" />.</exception>
	public bool Success => this.success ?? throw new InvalidOperationException("No result was recorded.");

	/// <summary>
	/// Gets the successful value when <see cref="Success"/> is true.
	/// If the result represents a failure, the recorded exception is thrown.
	/// </summary>
	/// <exception cref="Exception">The recorded exception is rethrown if the result represents a failure.</exception>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="IsDefault"/> is <see langword="true" />.</exception>
	public TValue Value => this.Success ? this.value! : throw new InvalidOperationException("No value was recorded.");

	/// <summary>
	/// Gets the recorded exception when <see cref="Success"/> is false.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when no error was recorded (when <see cref="Success"/> is true).</exception>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="IsDefault"/> is <see langword="true" />.</exception>
	public TError Error => !this.Success ? this.error! : throw new InvalidOperationException("No error was recorded.");

	/// <summary>
	/// Gets a  string used by the debugger to display the current result.
	/// </summary>
	private string DebuggerDisplay => this.IsDefault ? "(default)" : (this.Success ? $"Ok: {this.value}" : $"Err: {this.error}");

	/// <summary>
	/// Implicitly converts a value of <typeparamref name="TValue"/> into a successful <see cref="Result{TValue, TError}"/>.
	/// </summary>
	/// <param name="value">The value to wrap as a successful result.</param>
	public static implicit operator Result<TValue, TError>(TValue value) => Ok(value);

	/// <summary>
	/// Implicitly converts an <see cref="Exception"/> into a failed <see cref="Result{TValue, TError}"/>.
	/// </summary>
	/// <param name="error">The error to wrap as a failed result.</param>
	public static implicit operator Result<TValue, TError>(TError error) => Err(error);

	/// <summary>
	/// Creates a <see cref="Result{TValue, TError}"/> that represents a successful operation with the provided value.
	/// </summary>
	/// <param name="value">The successful result value.</param>
	/// <returns>A <see cref="Result{TValue, TError}"/> representing success.</returns>
	public static Result<TValue, TError> Ok(TValue value) => new(value);

	/// <summary>
	/// Creates a <see cref="Result{TValue, TError}"/> that represents a failed operation with the provided exception.
	/// </summary>
	/// <param name="error">The error that represents the failure.</param>
	/// <returns>A <see cref="Result{TValue, TError}"/> representing failure.</returns>
	public static Result<TValue, TError> Err(TError error) => new(error);

	/// <summary>
	/// Returns the successful value when this result represents success; otherwise returns the provided default value.
	/// </summary>
	/// <param name="defaultValue">The value to return if this result represents an error.</param>
	/// <returns>The successful value when <see cref="Success"/> is <see langword="true"/>, otherwise <paramref name="defaultValue"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="IsDefault"/> is <see langword="true"/>.</exception>
	public TValue GetValueOrDefault(TValue defaultValue) => this.Success ? this.Value : defaultValue;

	/// <inheritdoc/>
	public bool Equals(Result<TValue, TError> other)
	{
		if (this.success != other.success)
		{
			return false;
		}

		if (!this.success.HasValue)
		{
			return true;
		}

		return this.success.Value
			? EqualityComparer<TValue?>.Default.Equals(this.value, other.value)
			: EqualityComparer<TError?>.Default.Equals(this.error, other.error);
	}

	/// <summary>
	/// Transforms the successful value of this result using the specified <paramref name="mapper"/>.
	/// </summary>
	/// <typeparam name="TResult">The type of the value produced by the <paramref name="mapper"/>.</typeparam>
	/// <param name="mapper">A function that maps the successful value to a new value.</param>
	/// <returns>
	/// A <see cref="Result{TResult, TError}"/> containing the mapped value when this result is successful,
	/// otherwise a failed <see cref="Result{TResult, TError}"/> containing the original error.
	/// </returns>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="IsDefault"/> is <see langword="true"/>.</exception>
	public Result<TResult, TError> Map<TResult>(Func<TValue, TResult> mapper) => this.Success ? mapper(this.Value) : this.Error;

	/// <summary>
	/// Binds the successful value of this result to another operation that itself returns a <see cref="Result{TResult, TError}"/>.
	/// </summary>
	/// <typeparam name="TResult">The result type of the bound operation.</typeparam>
	/// <param name="binder">A function that, given the successful value, returns a new <see cref="Result{TResult, TError}"/>.</param>
	/// <returns>
	/// The <see cref="Result{TResult, TError}"/> returned by <paramref name="binder"/> when this result is successful,
	/// otherwise a failed <see cref="Result{TResult, TError}"/> containing the original error.
	/// </returns>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="IsDefault"/> is <see langword="true"/>.</exception>
	public Result<TResult, TError> Binder<TResult>(Func<TValue, Result<TResult, TError>> binder) => this.Success ? binder(this.Value) : this.Error;

	/// <summary>
	/// Matches on the result, invoking <paramref name="onSuccess"/> when successful or <paramref name="onError"/> when failed.
	/// </summary>
	/// <typeparam name="T">The return type of the match handlers.</typeparam>
	/// <param name="onSuccess">Function to run when the result represents success. Receives the successful value.</param>
	/// <param name="onError">Function to run when the result represents failure. Receives the recorded error.</param>
	/// <returns>The value returned by the invoked handler.</returns>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="IsDefault"/> is <see langword="true"/>.</exception>
	public T Match<T>(Func<TValue, T> onSuccess, Func<TError, T> onError) => this.Success ? onSuccess(this.Value) : onError(this.Error);
}
