// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Specialized;
using Nerdbank.MessagePack;
using PolyType;

NameValueCollection value = new() { { "Name", "Value" } };
MessagePackSerializer serializer = new MessagePackSerializer().WithNameValueCollectionConverter();
byte[] msgpack = serializer.Serialize(value, Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<NameValueCollection>());
NameValueCollection? roundtripped = serializer.Deserialize(msgpack, Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<NameValueCollection>());
if (roundtripped?["Name"] != "Value")
{
	throw new Exception("NameValueCollection did not roundtrip.");
}

Console.WriteLine("Success");

[GenerateShapeFor<NameValueCollection>]
internal partial class Witness;
