// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack;

/// <summary>
/// Extension methods for the <see cref="StructuralEqualityComparer"/> type.
/// </summary>
public static class StructuralEqualityComparerExtensions
{
	extension(StructuralEqualityComparer)
	{
		/// <summary>
		/// Gets a deep by-value equality comparer for the type <typeparamref name="T"/>, without hash collision resistance.
		/// </summary>
		/// <typeparam name="T">The type to be compared.</typeparam>
		/// <returns>The equality comparer.</returns>
		/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
		/// <remarks>
		/// <para>
		/// This overload should only be used when <typeparamref name="T"/> is decorated with a <see cref="GenerateShapeAttribute"/>.
		/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetDefault{T, TProvider}"/> instead,
		/// or use <see cref="StructuralEqualityComparer.GetDefault{T}(ITypeShapeProvider)"/> for an option that does not require source generation.
		/// </para>
		/// <para>
		/// See the remarks on the class for important notes about correctness of this implementation.
		/// </para>
		/// </remarks>
#if NET8_0
		[RequiresDynamicCode(MessagePackSerializerExtensions.ResolveDynamicMessage)]
#endif
#if NET
		[PreferDotNetAlternativeApi($"Use {nameof(StructuralEqualityComparer)}.{nameof(StructuralEqualityComparer.GetDefault)}<T> instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
#endif
		public static IEqualityComparer<T> GetDefault<T>()
			=> (IEqualityComparer<T>)StructuralEqualityComparer.DefaultEqualityComparerCache.GetOrAdd(TypeShapeResolver.ResolveDynamicOrThrow<T>())!;

		/// <summary>
		/// Gets a deep by-value equality comparer for the type <typeparamref name="T"/>, with hash collision resistance.
		/// </summary>
		/// <typeparam name="T">The type to be compared.</typeparam>
		/// <returns>The equality comparer.</returns>
		/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
		/// <remarks>
		/// <para>
		/// This overload should only be used when <typeparamref name="T"/> is decorated with a <see cref="GenerateShapeAttribute"/>.
		/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetHashCollisionResistant{T, TProvider}"/> instead,
		/// or use <see cref="StructuralEqualityComparer.GetHashCollisionResistant{T}(ITypeShapeProvider)"/> for an option that does not require source generation.
		/// </para>
		/// <para>
		/// See the remarks on the class for important notes about correctness of this implementation.
		/// </para>
		/// </remarks>
#if NET8_0
		[RequiresDynamicCode(MessagePackSerializerExtensions.ResolveDynamicMessage)]
#endif
#if NET
		[PreferDotNetAlternativeApi($"Use {nameof(StructuralEqualityComparer)}.{nameof(StructuralEqualityComparer.GetHashCollisionResistant)}<T> instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
#endif
		public static IEqualityComparer<T> GetHashCollisionResistant<T>()
			=> (IEqualityComparer<T>)StructuralEqualityComparer.HashCollisionResistantEqualityComparerCache.GetOrAdd(TypeShapeResolver.ResolveDynamicOrThrow<T>())!;

		/// <summary>
		/// Gets a deep by-value equality comparer for the type <typeparamref name="T"/>, without hash collision resistance.
		/// </summary>
		/// <typeparam name="T">The type to be compared.</typeparam>
		/// <typeparam name="TProvider">The witness type that provides the shape for <typeparamref name="T"/>.</typeparam>
		/// <returns>An equality comparer.</returns>
		/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
		/// <remarks>
		/// <para>
		/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
		/// Use <see cref="StructuralEqualityComparer.GetDefault{T}(ITypeShapeProvider)"/> for an option that does not require source generation.
		/// </para>
		/// <para>
		/// See the remarks on the class for important notes about correctness of this implementation.
		/// </para>
		/// </remarks>
#if NET8_0
		[RequiresDynamicCode(MessagePackSerializerExtensions.ResolveDynamicMessage)]
#endif
#if NET
		[PreferDotNetAlternativeApi($"Use {nameof(StructuralEqualityComparer)}.{nameof(StructuralEqualityComparer.GetDefault)}<T, TProvider> instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
#endif
		public static IEqualityComparer<T> GetDefault<T, TProvider>()
			=> (IEqualityComparer<T>)StructuralEqualityComparer.DefaultEqualityComparerCache.GetOrAdd(TypeShapeResolver.ResolveDynamicOrThrow<TProvider>())!;

		/// <summary>
		/// Gets a deep by-value equality comparer for the type <typeparamref name="T"/>, with hash collision resistance.
		/// </summary>
		/// <typeparam name="T">The type to be compared.</typeparam>
		/// <typeparam name="TProvider">The witness type that provides the shape for <typeparamref name="T"/>.</typeparam>
		/// <returns>An equality comparer.</returns>
		/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
		/// <remarks>
		/// <para>
		/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
		/// Use <see cref="StructuralEqualityComparer.GetHashCollisionResistant{T}(ITypeShapeProvider)"/> for an option that does not require source generation.
		/// </para>
		/// <para>
		/// See the remarks on the class for important notes about correctness of this implementation.
		/// </para>
		/// </remarks>
#if NET8_0
		[RequiresDynamicCode(MessagePackSerializerExtensions.ResolveDynamicMessage)]
#endif
#if NET
		[PreferDotNetAlternativeApi($"Use {nameof(StructuralEqualityComparer)}.{nameof(StructuralEqualityComparer.GetHashCollisionResistant)}<T, TProvider> instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
#endif
		public static IEqualityComparer<T> GetHashCollisionResistant<T, TProvider>()
			=> (IEqualityComparer<T>)StructuralEqualityComparer.HashCollisionResistantEqualityComparerCache.GetOrAdd(TypeShapeResolver.ResolveDynamicOrThrow<TProvider>())!;
	}
}
