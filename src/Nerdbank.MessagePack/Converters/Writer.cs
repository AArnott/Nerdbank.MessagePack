// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.PolySerializer.Converters;

public ref struct Writer
{
	private BufferWriter inner;

	internal Writer(BufferWriter writer)
	{
		this.inner = writer;
	}

	[UnscopedRef]
	public ref BufferWriter Buffer => ref this.inner;
}
