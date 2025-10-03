// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Nerdbank.MessagePack;

/// <summary>
/// Extension methods for the <see cref="Result{TValue, TError}"/> type.
/// </summary>
internal static class ResultExtensions
{
	/// <summary>
	/// Maps a <see cref="Result{TValue, TError}"/> to a <see cref="ConverterResult"/> using the specified <paramref name="mapper"/>.
	/// </summary>
	/// <typeparam name="T1">The type of result of the original value.</typeparam>
	/// <typeparam name="T2">The type of result coming from the <paramref name="mapper"/>.</typeparam>
	/// <param name="result">The original result.</param>
	/// <param name="mapper">The mapper.</param>
	/// <returns>The final result.</returns>
	internal static ConverterResult MapResult<T1, T2>(this Result<T1, VisitorError> result, Func<T1, MessagePackConverter<T2>> mapper)
		=> result.Success ? ConverterResult.Ok(mapper(result.Value)) : ConverterResult.Err(result.Error);

	/// <summary>
	/// Gets the value from a <see cref="Result{TValue, TError}"/> or throws an exception for the error condition.
	/// </summary>
	/// <typeparam name="T">The type of value to be returned.</typeparam>
	/// <param name="result">The result to evaluate.</param>
	/// <returns>The value.</returns>
	internal static T GetValueOrThrow<T>(this Result<T, VisitorError> result)
		=> result.Success ? result.Value : throw result.Error.ThrowException();
}
