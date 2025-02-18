// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift.Converters;

internal interface IReferencePreservingManager
{
	Converter<T> WrapWithReferencePreservingConverter<T>(Converter<T> converter);

	Converter<T> UnwrapFromReferencePreservingConverter<T>(Converter<T> converter);
}

internal static class ReferencePreservingManagerExtensions
{
	internal static Converter WrapWithReferencePreservingConverter(this IReferencePreservingManager manager, Converter converter) => converter.Invoke(ConverterWrapper.Instance, manager);

	internal static Converter UnwrapFromReferencePreservingConverter(this IReferencePreservingManager manager, Converter converter) => converter.Invoke(ConverterUnwrapper.Instance, manager);

	private class ConverterWrapper : ITypedConverterInvoke<IReferencePreservingManager, Converter>
	{
		internal static readonly ConverterWrapper Instance = new();

		private ConverterWrapper() { }

		public Converter Invoke<T>(Converter<T> converter, IReferencePreservingManager manager) => manager.WrapWithReferencePreservingConverter(converter);
	}

	private class ConverterUnwrapper : ITypedConverterInvoke<IReferencePreservingManager, Converter>
	{
		internal static readonly ConverterUnwrapper Instance = new();

		private ConverterUnwrapper() { }

		public Converter Invoke<T>(Converter<T> converter, IReferencePreservingManager manager) => manager.UnwrapFromReferencePreservingConverter(converter);
	}
}
