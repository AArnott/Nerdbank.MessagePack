// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A factory for <see cref="MessagePackConverter{T}"/> objects of arbitrary types.
/// </summary>
/// <example>
/// <para>
/// A non-generic implementation of this interface is preferred when possible.
/// </para>
/// <code source="../../samples/cs/CustomConverterFactory.cs" region="NonGeneric" lang="C#" />
/// <para>
/// When a generic context is required, implement <see cref="ITypeShapeFunc"/> on the same class
/// and invoke into it after appropriate type checks.
/// </para>
/// <code source="../../samples/cs/CustomConverterFactory.cs" region="Generic" lang="C#" />
/// <para>
/// When generic type parameters are required for sub-elements of the type to be converted
/// (e.g. the element type of a collection), you can leverage a <see cref="TypeShapeVisitor"/> implementation
/// to obtain the generic type parameters.
/// </para>
/// <code source="../../samples/cs/CustomConverterFactory.cs" region="Visitor" lang="C#" />
/// </example>
public interface IMessagePackConverterFactory
{
	/// <summary>
	/// Creates a converter for the given type if this factory is capable of it.
	/// </summary>
	/// <param name="type">The type to be serialized.</param>
	/// <param name="shape">
	/// The shape of the type to be serialized, if available.
	/// The shape will typically be available.
	/// The only known exception is when the type has a
	/// <see cref="TypeShapeAttribute.Marshaler">PolyType marshaler</see> defined.
	/// </param>
	/// <param name="context">The context in which this factory is being invoked. Provides access to other converters that may be required by the requested converter.</param>
	/// <returns>The converter for the data type, or <see langword="null" />.</returns>
	/// <remarks>
	/// Implementations that require a generic type parameter for the type to be converted should
	/// also implement <see cref="ITypeShapeFunc"/> with an <see cref="ITypeShapeFunc.Invoke{T}(ITypeShape{T}, object?)"/>
	/// method that creates the converter.
	/// The implementation of <em>this</em> method should perform any type checks necessary
	/// to determine whether this factory applies to the given shape, and if so,
	/// call <see cref="MessagePackConverterFactoryExtensions.Invoke{T}(T, ITypeShape, object?)"/>
	/// to forward the call to the generic <see cref="ITypeShapeFunc.Invoke{T}(ITypeShape{T}, object?)"/> method
	/// defined on that same class.
	/// </remarks>
	MessagePackConverter? CreateConverter(Type type, ITypeShape? shape, in ConverterContext context);
}

/// <summary>
/// Extension methods for the <see cref="IMessagePackConverterFactory"/> interface.
/// </summary>
public static class MessagePackConverterFactoryExtensions
{
	/// <summary>
	/// Calls the <see cref="ITypeShapeFunc.Invoke{T}(ITypeShape{T}, object?)"/> method on the given factory.
	/// </summary>
	/// <typeparam name="T">The concrete <see cref="IMessagePackConverterFactory"/> type.</typeparam>
	/// <param name="self">The instance of the factory.</param>
	/// <param name="shape">The shape to create a converter for.</param>
	/// <param name="state">Optional state to pass onto the inner method.</param>
	/// <returns>The converter.</returns>
	public static MessagePackConverter? Invoke<T>(this T self, ITypeShape shape, object? state = null)
		where T : IMessagePackConverterFactory, ITypeShapeFunc
	{
		return (MessagePackConverter?)Requires.NotNull(shape).Invoke(self, state);
	}
}
