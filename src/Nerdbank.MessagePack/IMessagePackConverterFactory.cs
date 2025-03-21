// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// A factory for <see cref="MessagePackConverter{T}"/> objects of arbitrary types.
/// </summary>
public interface IMessagePackConverterFactory
{
	/// <summary>
	/// Creates a converter for the given type if this factory is capable of it.
	/// </summary>
	/// <typeparam name="T">The data type.</typeparam>
	/// <returns>The converter for the data type, or <see langword="null" />.</returns>
	MessagePackConverter<T>? CreateConverter<T>();
}
