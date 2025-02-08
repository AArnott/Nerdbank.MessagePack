// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer;

/// <summary>
/// An interface that may be implemented to receive callbacks before serialization and after deserialization.
/// </summary>
public interface IMessagePackSerializationCallbacks
{
	/// <summary>
	/// Performs any additional operations on an object before it is serialized.
	/// </summary>
	void OnBeforeSerialize()
#if NET
	{
	}
#else
	;
#endif

	/// <summary>
	/// Performs any additional operations on an object after it has been deserialized.
	/// </summary>
	void OnAfterDeserialize()
#if NET
	{
	}
#else
	;
#endif
}
