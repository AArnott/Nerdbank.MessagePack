
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

internal sealed class ScratchBuffer : IDisposable
{
	private const int DefaultCapacity = 64;

	private byte[]? rented;
	private int count;

	public int Count
	{
		get => this.count;
		set
		{
			if ((uint)value >= this.count && (uint)value <= this.rented?.Length)
			{
				this.count = value;
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(value));
			}
		}
	}

	public int Capacity => this.rented?.Length ?? 0;

	public Span<byte> BufferSpan => this.rented ?? default;

	public Span<byte> Span => this.count == 0 ? default : this.BufferSpan.Slice(0, this.count);

	public void Add(byte value)
	{
		Span<byte> buffer = this.BufferSpan;
		int count = this.Count;
		if ((uint)count < (uint)buffer.Length)
		{
			this.Count = count + 1;
			buffer[count] = value;
		}
		else
		{
			this.AddSlow(value);
		}
	}

	public void Dispose()
	{
		this.Clear();
		if (this.rented is not null)
		{
			ArrayPool<byte>.Shared.Return(this.rented);
			this.rented = null;
		}
	}

	public void AddRange(ReadOnlySpan<byte> span)
	{
		if (!span.IsEmpty)
		{
			Span<byte> buffer = this.BufferSpan;
			int count = this.Count;
			if (buffer.Length - count < span.Length)
			{
				this.EnsureCapacity(checked(count + span.Length));
				buffer = this.BufferSpan;
			}

			span.CopyTo(buffer[count..]);
			this.Count = count + span.Length;
		}
	}

	public void EnsureCapacity(int capacity)
	{
		Debug.Assert(capacity >= 0);

		if (this.BufferSpan.Length < capacity)
		{
			this.Grow(this.GetNewCapacity(capacity));
		}
	}

	public void Clear()
	{
		this.count = 0;
	}

	private int GetNewCapacity(int capacity)
	{
		Span<byte> buffer = this.BufferSpan;
		Debug.Assert(buffer.Length < capacity);

		int newCapacity = buffer.Length == 0 ? DefaultCapacity : buffer.Length * 2;

		if ((uint)newCapacity < (uint)capacity)
		{
			newCapacity = capacity;
		}

		return newCapacity;
	}

	private void Grow(int newSize)
	{
		byte[] newArray = ArrayPool<byte>.Shared.Rent(newSize);
		this.BufferSpan.CopyTo(newArray);
		if (this.rented is not null)
		{
			ArrayPool<byte>.Shared.Return(this.rented);
		}

		this.rented = newArray;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void AddSlow(byte value)
	{
		this.EnsureCapacity(this.Count + 1);
		this.BufferSpan[this.Count++] = value;
	}
}
