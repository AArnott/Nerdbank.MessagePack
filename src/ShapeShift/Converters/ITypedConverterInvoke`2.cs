// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift.Converters;

/// <summary>
/// A callback interface for users of <see cref="Converter.Invoke{TState, TResult}(ITypedConverterInvoke{TState, TResult}, TState)"/>
/// in order to invoke a generic method from a non-generic one, closing the generic with the generic type of the <see cref="Converter{T}"/>.
/// </summary>
/// <typeparam name="TState">The type of argument passed between the caller and the invoked method.</typeparam>
/// <typeparam name="TResult">The type of result that will pass back from the invoked method to its caller.</typeparam>
internal interface ITypedConverterInvoke<TState, TResult>
{
	/// <summary>
	/// Performs some arbitrary operation with a generic type argument that matches the type argument from some <see cref="Converter{T}"/>.
	/// </summary>
	/// <typeparam name="T">The data type that may be converted.</typeparam>
	/// <param name="converter">The converter on which <see cref="Converter.Invoke{TState, TResult}(ITypedConverterInvoke{TState, TResult}, TState)"/> was invoked.</param>
	/// <param name="state">The state provided to <see cref="Converter.Invoke{TState, TResult}(ITypedConverterInvoke{TState, TResult}, TState)"/>.</param>
	/// <returns>The result value to pass to the caller of <see cref="Converter.Invoke{TState, TResult}(ITypedConverterInvoke{TState, TResult}, TState)"/>.</returns>
	TResult Invoke<T>(Converter<T> converter, TState state);
}
