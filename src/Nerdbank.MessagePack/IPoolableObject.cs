// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Represents an object that can be recycled for reuse.
/// </summary>
internal interface IPoolableObject
{
	/// <summary>
	/// Gets or sets the owner of this object, for the time it is in use.
	/// </summary>
	internal MessagePackSerializer? Owner { get; set; }

	/// <summary>
	/// Clears the object's state so that it can be reused.
	/// </summary>
	internal void Recycle();
}
