// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Provides constants related to DateTime operations.
/// </summary>
internal static class DateTimeConstants
{
	/// <summary>
	/// The number of seconds at the Unix epoch (1970-01-01T00:00:00Z) according to the BCL (Base Class Library).
	/// </summary>
	internal const long BclSecondsAtUnixEpoch = 62135596800;

	/// <summary>
	/// The number of nanoseconds per tick. A tick is 100 nanoseconds.
	/// </summary>
	internal const int NanosecondsPerTick = 100;

	/// <summary>
	/// The Unix epoch (1970-01-01T00:00:00Z) as a <see cref="DateTime"/> object.
	/// </summary>
	internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
