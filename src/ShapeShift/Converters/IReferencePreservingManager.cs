// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift.Converters;

/// <summary>
/// Wraps and unwraps <see cref="Converter{T}"/> objects to preserve reference equality.
/// </summary>
internal interface IReferencePreservingManager
{
	/// <summary>
	/// Wraps a converter with one that will replace an object with a reference if it was serialized before,
	/// and replace the reference with the object if it was deserialized before.
	/// </summary>
	/// <typeparam name="T">The type of class to be serialized.</typeparam>
	/// <param name="converter">The converter for the data type.</param>
	/// <returns>The wrapping converter.</returns>
	Converter<T> WrapWithReferencePreservingConverter<T>(Converter<T> converter);

	/// <summary>
	/// Returns the original converter supplied to <see cref="WrapWithReferencePreservingConverter{T}(Converter{T})"/>
	/// given a wrapping converter.
	/// </summary>
	/// <typeparam name="T">The type of the class to be serialized.</typeparam>
	/// <param name="converter">A converter, which may be a wrapping converter.</param>
	/// <returns>The original converter if <paramref name="converter"/> is a wrapper, otherwise <paramref name="converter"/> itself.</returns>
	Converter<T> UnwrapFromReferencePreservingConverter<T>(Converter<T> converter);
}

/// <summary>
/// Extension methods for the <see cref="IReferencePreservingManager"/> interface.
/// </summary>
internal static class ReferencePreservingManagerExtensions
{
	/// <inheritdoc cref="IReferencePreservingManager.WrapWithReferencePreservingConverter{T}(Converter{T})"/>
	internal static Converter WrapWithReferencePreservingConverter(this IReferencePreservingManager manager, Converter converter) => converter.Invoke(ConverterWrapper.Instance, manager);

	/// <inheritdoc cref="IReferencePreservingManager.UnwrapFromReferencePreservingConverter{T}(Converter{T})"/>
	internal static Converter UnwrapFromReferencePreservingConverter(this IReferencePreservingManager manager, Converter converter) => converter.Invoke(ConverterUnwrapper.Instance, manager);

	private class ConverterWrapper : ITypedConverterInvoke<IReferencePreservingManager, Converter>
	{
		internal static readonly ConverterWrapper Instance = new();

		private ConverterWrapper()
		{
		}

		public Converter Invoke<T>(Converter<T> converter, IReferencePreservingManager manager) => manager.WrapWithReferencePreservingConverter(converter);
	}

	private class ConverterUnwrapper : ITypedConverterInvoke<IReferencePreservingManager, Converter>
	{
		internal static readonly ConverterUnwrapper Instance = new();

		private ConverterUnwrapper()
		{
		}

		public Converter Invoke<T>(Converter<T> converter, IReferencePreservingManager manager) => manager.UnwrapFromReferencePreservingConverter(converter);
	}
}
