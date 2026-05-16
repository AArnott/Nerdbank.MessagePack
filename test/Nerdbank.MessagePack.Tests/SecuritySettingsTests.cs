// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class SecuritySettingsTests
{
	[Fact]
	public void CustomSettings()
	{
		SecuritySettings settings = new()
		{
			MaxCollectionPreallocation = 1024,
		};
		Assert.Equal(1024, settings.MaxCollectionPreallocation);
	}

	[Fact]
	public void DefaultCtorMatchesUntrustedData()
	{
		Assert.Equal(SecuritySettings.UntrustedData, new SecuritySettings());
	}

	[Fact]
	public void MaxCollectionPreallocation_RequiresPositiveValue()
	{
		SecuritySettings value = SecuritySettings.UntrustedData with
		{
			MaxCollectionPreallocation = 1,
		};
		Assert.Throws<ArgumentOutOfRangeException>(() => SecuritySettings.UntrustedData with
		{
			MaxCollectionPreallocation = 0,
		});
		Assert.Throws<ArgumentOutOfRangeException>(() => SecuritySettings.UntrustedData with
		{
			MaxCollectionPreallocation = -1,
		});
	}
}
