// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Extension methods for the <see cref="MessagePackSerializer"/> class and related types.
/// </summary>
/// <remarks>
/// <para>
/// Some of these methods will be very typical when targeting .NET Standard or .NET Framework,
/// but when targeting .NET, the compiler will prefer the instance methods on the
/// <see cref="MessagePackSerializer"/> class itself.
/// These instance methods are faster and produce compile errors rather than runtime exceptions
/// that may be thrown by these extension methods.
/// </para>
/// </remarks>
public static partial class MessagePackSerializerExtensions
{
#if NET8_0
	/// <summary>
	/// A message to use as the argument to <see cref="RequiresDynamicCodeAttribute"/>
	/// for methods that call into <see cref="TypeShapeResolver.ResolveDynamicOrThrow{T}"/>.
	/// </summary>
	/// <seealso href="https://github.com/dotnet/runtime/issues/119440#issuecomment-3269894751"/>
	internal const string ResolveDynamicMessage =
		"Dynamic resolution of IShapeable<T> interface may require dynamic code generation in .NET 8 Native AOT. " +
		"It is recommended to switch to statically resolved IShapeable<T> APIs or upgrade your app to .NET 9 or later.";
#endif

	/// <summary>
	/// <inheritdoc cref="MessagePackSerializer.GetJsonSchema(ITypeShape)" path="/summary"/>
	/// </summary>
	/// <typeparam name="T">The self-describing type whose schema should be produced.</typeparam>
	/// <param name="self">The serializer.</param>
	/// <returns><inheritdoc cref="MessagePackSerializer.GetJsonSchema(ITypeShape)" path="/returns"/></returns>
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with a <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use <see cref="MessagePackSerializer.GetJsonSchema{T}(ITypeShapeProvider)"/> for an option that does not require source generation.
	/// </remarks>
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static JsonObject GetJsonSchema<T>(this MessagePackSerializer self)
		=> Requires.NotNull(self).GetJsonSchema(TypeShapeResolver.ResolveDynamicOrThrow<T>());

	/// <summary>
	/// <inheritdoc cref="MessagePackSerializer.GetJsonSchema(ITypeShape)" path="/summary"/>
	/// </summary>
	/// <typeparam name="T">The type whose schema should be produced.</typeparam>
	/// <typeparam name="TProvider">The witness type that provides the shape for <typeparamref name="T"/>.</typeparam>
	/// <param name="self">The serializer.</param>
	/// <returns><inheritdoc cref="MessagePackSerializer.GetJsonSchema(ITypeShape)" path="/returns"/></returns>
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use <see cref="MessagePackSerializer.GetJsonSchema{T}(ITypeShapeProvider)"/> for an option that does not require source generation.
	/// </remarks>
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static JsonObject GetJsonSchema<T, TProvider>(this MessagePackSerializer self)
		=> Requires.NotNull(self).GetJsonSchema(TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>());

	/// <inheritdoc cref="SerializationContext.GetConverter{T}(ITypeShape{T})"/>
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with a <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetConverter{T, TProvider}"/> instead,
	/// or use <see cref="SerializationContext.GetConverter{T}(ITypeShapeProvider?)"/> for an option that does not require source generation.
	/// </remarks>
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static MessagePackConverter<T> GetConverter<T>(this SerializationContext context)
		=> context.GetConverter(TypeShapeResolver.ResolveDynamicOrThrow<T>());

	/// <inheritdoc cref="SerializationContext.GetConverter{T}(ITypeShape{T})"/>
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use <see cref="SerializationContext.GetConverter{T}(ITypeShapeProvider?)"/> for an option that does not require source generation.
	/// </remarks>
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static MessagePackConverter<T> GetConverter<T, TProvider>(this SerializationContext context)
		=> context.GetConverter(TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>());
}
