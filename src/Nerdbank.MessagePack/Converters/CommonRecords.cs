// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

internal delegate void SerializeProperty<TDeclaringType>(ref TDeclaringType container, ref MessagePackWriter writer);

internal delegate void DeserializeProperty<TDeclaringType>(ref TDeclaringType container, ref MessagePackReader reader);

internal record struct MapSerializableProperties<T>(List<(ReadOnlyMemory<byte> RawPropertyNameString, SerializeProperty<T> Write)> Properties);

internal record struct MapDeserializableProperties<T>(SpanDictionary<byte, DeserializeProperty<T>> Readers);

internal record struct PropertyData<T>(SpanDictionary<byte, DeserializeProperty<T>?> PropertyReaders, List<(byte[] RawPropertyNameString, byte[] PropertyNameUtf8, PropertyAccessors<T> Accessors)> Properties);

internal record struct PropertyAccessors<TDeclaringType>(SerializeProperty<TDeclaringType>? Serialize, DeserializeProperty<TDeclaringType>? Deserialize);
