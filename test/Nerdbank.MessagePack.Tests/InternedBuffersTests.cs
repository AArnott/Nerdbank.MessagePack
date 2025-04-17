// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft;

public class InternedBuffersTests
{
	private readonly InternedBuffers intern = new();

	[Fact]
	public void EqualSpansProduceRefEqualMemory()
	{
		Assert.True(this.intern.Intern([1, 2, 3]).Span == this.intern.Intern([1, 2, 3]).Span);
	}

	[Fact]
	public void NonEqualSpansProduceUniqueMemory()
	{
		Assert.False(this.intern.Intern([1, 2, 4]).Span == this.intern.Intern([1, 2, 3]).Span);
	}

	[Fact]
	public void InterningStoredInIsolation()
	{
		InternedBuffers intern2 = new();
		Assert.False(this.intern.Intern([1, 2, 3]).Span == intern2.Intern([1, 2, 3]).Span);
	}

	[Fact]
	public void InternSeveralBuffers()
	{
		// This verifies that several buffers can be interned and retrieved at once.
		// However there is a risk that this test will be unstable because
		// the interning data structure only stores one buffer per hash code.
		// If the hash function changes (for security reasons, like the string hash function does)
		// for each process, then these buffers just _might_ have a hash collision in a presumably
		// very rare circumstance.
		ReadOnlyMemory<byte> memory1 = this.intern.Intern([1, 2, 3]);
		ReadOnlyMemory<byte> memory2 = this.intern.Intern([4, 5, 6]);
		ReadOnlyMemory<byte> memory3 = this.intern.Intern([7, 8, 9]);
		ReadOnlyMemory<byte> memory1b = this.intern.Intern([1, 2, 3]);
		Assert.False(memory1.Span == memory2.Span);
		Assert.False(memory1.Span == memory3.Span);
		Assert.False(memory2.Span == memory3.Span);
		Assert.True(memory1.Span == memory1b.Span);
	}

	[Fact]
	public void BuffersAreWeaklyRetained()
	{
		WeakReference<byte[]> weakRef = Helper();
		GC.Collect();
		Assert.False(weakRef.TryGetTarget(out _));

		[MethodImpl(MethodImplOptions.NoInlining)]
		WeakReference<byte[]> Helper()
		{
			ReadOnlyMemory<byte> memory = this.intern.Intern([1, 2, 3]);
			Assumes.True(MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment));
			return new(segment.Array!);
		}
	}
}
