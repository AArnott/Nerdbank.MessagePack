// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class ExtensionTests
{
	private Extension ext1a = new(1, new byte[] { 1, 2, 3 });
	private Extension ext1b = new(1, new byte[] { 1, 2, 3 });
	private Extension ext2 = new(2, new byte[] { 1, 2, 3, 4, 5 });
	private Extension ext3 = new(1, new byte[] { 1, 2, 4, 5 });

	[Test]
	public void Equality()
	{
		Assert.Equal(this.ext1a, this.ext1b);
		Assert.NotEqual(this.ext1a, this.ext2);
		Assert.NotEqual(this.ext1a, this.ext3);
	}

	[Test]
	public void GetHashCode_Overridden()
	{
		Assert.Equal(this.ext1a.GetHashCode(), this.ext1b.GetHashCode());
		Assert.NotEqual(this.ext1a.GetHashCode(), this.ext2.GetHashCode());
		Assert.NotEqual(this.ext1a.GetHashCode(), this.ext3.GetHashCode());
	}

	[Test]
	public void GetSecureHashCode_StructuralEquality()
	{
		// The secure hash code should be the same for two structurally equal extensions.
		Assert.Equal(GetSecureHashCode(this.ext1a), GetSecureHashCode(this.ext1b));
		Assert.NotEqual(GetSecureHashCode(this.ext1a), GetSecureHashCode(this.ext2));
		Assert.NotEqual(GetSecureHashCode(this.ext1a), GetSecureHashCode(this.ext3));
	}

	private static long GetSecureHashCode<T>(T expected)
		where T : IStructuralSecureEqualityComparer<T> => expected.GetSecureHashCode();
}
