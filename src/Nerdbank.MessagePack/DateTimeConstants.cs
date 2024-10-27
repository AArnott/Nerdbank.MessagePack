// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

internal static class DateTimeConstants
{
	internal const long BclSecondsAtUnixEpoch = 62135596800;
	internal const int NanosecondsPerTick = 100;
	internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
