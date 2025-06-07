// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

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
	/// Performs any additional operations on an object after it is serialized.
	/// </summary>
	void OnAfterSerialize()
#if NET
	{
	}
#else
	;
#endif

	/// <summary>
	/// Performs any additional operations on an object before any properties are set.
	/// </summary>
	/// <remarks>
	/// A converter may not call this method if it is not supported.
	/// In particular, types with <see langword="required" /> or <see langword="init" /> properties
	/// or a deserializing constructor should expect this method to not be called
	/// if the default converter is used.
	/// </remarks>
	void OnBeforeDeserialize()
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
