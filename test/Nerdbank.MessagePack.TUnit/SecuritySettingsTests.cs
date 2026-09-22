// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class SecuritySettingsTests
{
	[Test]
	public void CustomSettings()
	{
		SecuritySettings settings = new()
		{
			ExpandoObjectMaxPropertyCount = 512,
			MaxCollectionPreallocation = 1024,
		};
		Assert.Equal(512, settings.ExpandoObjectMaxPropertyCount);
		Assert.Equal(1024, settings.MaxCollectionPreallocation);
	}

	[Test]
	public void DefaultCtorMatchesUntrustedData()
	{
		Assert.Equal(SecuritySettings.UntrustedData, new SecuritySettings());
	}

	[Test]
	public void ExpandoObjectMaxPropertyCount_RequiresPositiveValue()
	{
		SecuritySettings value = SecuritySettings.UntrustedData with
		{
			ExpandoObjectMaxPropertyCount = 1,
		};
		Assert.Throws<ArgumentOutOfRangeException>(() => SecuritySettings.UntrustedData with
		{
			ExpandoObjectMaxPropertyCount = 0,
		});
		Assert.Throws<ArgumentOutOfRangeException>(() => SecuritySettings.UntrustedData with
		{
			ExpandoObjectMaxPropertyCount = -1,
		});
	}

	[Test]
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
