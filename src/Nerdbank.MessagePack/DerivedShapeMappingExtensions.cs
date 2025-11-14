// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1101 // https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3954

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack;

/// <summary>
/// Extension members for the <see cref="DerivedShapeMapping{TBase}"/> class.
/// </summary>
public static class DerivedShapeMappingExtensions
{
	extension<TBase>(DerivedShapeMapping<TBase> self)
	{
		/// <inheritdoc cref="DerivedShapeMapping{TBase}.Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})" />
		/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TDerived"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
		/// <remarks>
		/// This overload should only be used when <typeparamref name="TDerived"/> is decorated with a <see cref="GenerateShapeAttribute"/>.
		/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="Add{TBase, TDerived, TProvider}(DerivedShapeMapping{TBase}, DerivedTypeIdentifier)"/> instead,
		/// or use <see cref="DerivedShapeMapping{TBase}.Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})"/> for an option that does not require source generation.
		/// </remarks>
#if NET8_0
		[RequiresDynamicCode(MessagePackSerializerExtensions.ResolveDynamicMessage)]
#endif
#if NET
		[PreferDotNetAlternativeApi(UseDotNetAlternativeMessage)]
		[EditorBrowsable(EditorBrowsableState.Never)]
#endif
		public void Add<TDerived>(DerivedTypeIdentifier alias)
			where TDerived : TBase => self.Add(alias, TypeShapeResolver.ResolveDynamicOrThrow<TDerived>());

		/// <inheritdoc cref="DerivedShapeMapping{TBase}.Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})" path="/summary" />
		/// <inheritdoc cref="DerivedShapeMapping{TBase}.Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})" path="/exception" />
		/// <param name="alias"><inheritdoc cref="DerivedShapeMapping{TBase}.Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})" path="/param[@name='alias']" /></param>
		/// <typeparam name="TDerived"><inheritdoc cref="DerivedShapeMapping{TBase}.Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})" path="/typeparam[@name='TDerived']"/></typeparam>
		/// <typeparam name="TProvider">The witness class that provides a type shape for <typeparamref name="TDerived"/>.</typeparam>
		/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="TDerived"/>.</exception>
		/// <remarks>
		/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
		/// Use <see cref="DerivedShapeMapping{TBase}.Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})"/> for an option that does not require source generation.
		/// </remarks>
#if NET8_0
		[RequiresDynamicCode(MessagePackSerializerExtensions.ResolveDynamicMessage)]
#endif
#if NET
		[PreferDotNetAlternativeApi(UseDotNetAlternativeMessage)]
		[EditorBrowsable(EditorBrowsableState.Never)]
#endif
		public void Add<TDerived, TProvider>(DerivedTypeIdentifier alias)
			where TDerived : TBase
			=> self.Add(alias, TypeShapeResolver.ResolveDynamicOrThrow<TDerived, TProvider>());
	}

#if NET
	private const string UseDotNetAlternativeMessage = "Use the Add method instead.";
#endif
}
