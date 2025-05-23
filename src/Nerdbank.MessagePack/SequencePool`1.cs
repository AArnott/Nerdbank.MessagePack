﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/MessagePack-CSharp/MessagePack-CSharp/
namespace Nerdbank.MessagePack;

/// <summary>
/// A pool of <see cref="Sequence{T}"/> objects.
/// </summary>
/// <typeparam name="T">The type of elements in the sequences.</typeparam>
internal class SequencePool<T>
{
	/// <summary>
	/// A thread-safe pool of reusable <see cref="Sequence{T}"/> objects.
	/// </summary>
	internal static readonly SequencePool<T> Shared = new SequencePool<T>();

	/// <summary>
	/// The value to use for <see cref="Sequence{T}.MinimumSpanLength"/>.
	/// </summary>
	/// <remarks>
	/// Individual users that want a different value for this can modify the setting on the rented <see cref="Sequence{T}"/>
	/// or by supplying their own <see cref="IBufferWriter{T}" />.
	/// </remarks>
	/// <devremarks>
	/// We use 32KB so that when LZ4Codec.MaximumOutputLength is used on this length it does not require a
	/// buffer that would require the Large Object Heap.
	/// </devremarks>
	private const int MinimumSpanLength = 32 * 1024;

	private readonly int maxSize;
	private readonly Stack<Sequence<T>> pool = new Stack<Sequence<T>>();

	/// <summary>
	/// The array pool which we share with all <see cref="Sequence{T}"/> objects created by this <see cref="SequencePool{T}"/> instance.
	/// </summary>
	private readonly ArrayPool<T> arrayPool;

	/// <summary>
	/// Initializes a new instance of the <see cref="SequencePool{T}"/> class.
	/// </summary>
	/// <remarks>
	/// We use a <see cref="maxSize"/> that allows every processor to be involved in messagepack serialization concurrently,
	/// plus one nested serialization per processor (since LZ4 and sometimes other nested serializations may exist).
	/// </remarks>
	public SequencePool()
		: this(Environment.ProcessorCount * 2, ArrayPool<T>.Create(80 * 1024, 100))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SequencePool{T}"/> class.
	/// </summary>
	/// <param name="maxSize">The maximum size to allow the pool to grow.</param>
	/// <devremarks>
	/// We allow 100 arrays to be shared (instead of the default 50) and reduce the max array length from the default 1MB to something more reasonable for our expected use.
	/// </devremarks>
	public SequencePool(int maxSize)
		: this(maxSize, ArrayPool<T>.Create(80 * 1024, 100))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SequencePool{T}"/> class.
	/// </summary>
	/// <param name="maxSize">The maximum size to allow the pool to grow.</param>
	/// <param name="arrayPool">Array pool that will be used.</param>
	public SequencePool(int maxSize, ArrayPool<T> arrayPool)
	{
		this.maxSize = maxSize;
		this.arrayPool = arrayPool;
	}

	/// <summary>Clears the pool, letting the <see cref="GC"/> collect the recycled buffers.</summary>
	public void Clear()
	{
		lock (this.pool)
		{
			this.pool.Clear();
		}
	}

	/// <summary>
	/// Gets an instance of <see cref="Sequence{T}"/>
	/// This is taken from the recycled pool if one is available; otherwise a new one is created.
	/// </summary>
	/// <returns>The rental tracker that provides access to the object as well as a means to return it.</returns>
	internal Rental Rent()
	{
		lock (this.pool)
		{
			if (this.pool.Count > 0)
			{
				return new Rental(this, this.pool.Pop());
			}
		}

		// Configure the newly created object to share a common array pool with the other instances,
		// otherwise each one will have its own ArrayPool which would likely waste a lot of memory.
		return new Rental(this, new Sequence<T>(this.arrayPool) { MinimumSpanLength = MinimumSpanLength });
	}

	private void Return(Sequence<T> value)
	{
		value.Reset();
		lock (this.pool)
		{
			if (this.pool.Count < this.maxSize)
			{
				// Reset to preferred settings in case the renter changed them.
				value.MinimumSpanLength = MinimumSpanLength;

				this.pool.Push(value);
			}
		}
	}

	/// <summary>
	/// A struct that manages the rental of a <see cref="Sequence{T}"/> from a <see cref="SequencePool{T}"/>.
	/// </summary>
	internal struct Rental : IDisposable
	{
		private readonly SequencePool<T> owner;
		private Sequence<T>? value;

		/// <summary>
		/// Initializes a new instance of the <see cref="Rental"/> struct.
		/// </summary>
		/// <param name="owner">The <see cref="SequencePool{T}"/> that owns this rental.</param>
		/// <param name="value">The <see cref="Sequence{T}"/> object being rented.</param>
		internal Rental(SequencePool<T> owner, Sequence<T> value)
		{
			this.owner = owner;
			this.value = value;
		}

		/// <summary>
		/// Gets the recyclable object.
		/// </summary>
		public Sequence<T> Value => this.value ?? throw new ObjectDisposedException(nameof(Rental));

		/// <summary>
		/// Returns the recyclable object to the pool.
		/// </summary>
		/// <remarks>
		/// The instance is cleaned first, if a clean delegate was provided.
		/// It is dropped instead of being returned to the pool if the pool is already at its maximum size.
		/// </remarks>
		public void Dispose()
		{
			this.owner?.Return(this.Value);
			this.value = null;
		}
	}
}
