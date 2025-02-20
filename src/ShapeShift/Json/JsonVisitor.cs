// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType.Utilities;

namespace ShapeShift.Json;

/// <summary>
/// A JSON-specific implementation of <see cref="StandardVisitor"/>.
/// </summary>
/// <param name="owner"><inheritdoc cref="StandardVisitor.StandardVisitor(ShapeShift.ConverterCache, TypeGenerationContext)" path="/param[@name='owner']"/></param>
/// <param name="context"><inheritdoc cref="StandardVisitor.StandardVisitor(ShapeShift.ConverterCache, TypeGenerationContext)" path="/param[@name='context']"/></param>
internal class JsonVisitor(ConverterCache owner, TypeGenerationContext context)
	: StandardVisitor(owner, context)
{
	protected override Converter<T> CreateObjectMapConverter<T>(MapSerializableProperties<T> serializable, MapDeserializableProperties<T>? deserializable, Func<T>? ctor, SerializeDefaultValuesPolicy defaultValuesPolicy)
	 => new ObjectMapConverter<T>(serializable, deserializable, ctor, defaultValuesPolicy, (JsonFormatter)this.ConverterCache.Formatter);

	protected override Converter<TDeclaringType> CreateObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(MapSerializableProperties<TDeclaringType> serializable, Func<TArgumentState> argStateCtor, Constructor<TArgumentState, TDeclaringType> ctor, MapDeserializableProperties<TArgumentState> parameters, SerializeDefaultValuesPolicy defaultValuesPolicy)
		=> new ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(serializable, argStateCtor, ctor, parameters, defaultValuesPolicy, (JsonFormatter)this.ConverterCache.Formatter);
}
