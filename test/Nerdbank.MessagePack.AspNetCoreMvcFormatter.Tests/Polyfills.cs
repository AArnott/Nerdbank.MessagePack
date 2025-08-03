// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static class Polyfills
{
#if !NET
	internal static Task CopyToAsync(this Stream source, Stream destination, CancellationToken cancellationToken = default)
		=> source.CopyToAsync(destination, 16 * 1024, cancellationToken);
#endif
}
