// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Utilities;

internal static class PolyTypeUtilities
{
	internal static ITypeShape<T> GetShape<T, TProvider>()
		where TProvider : IShapeable<T> => TProvider.GetShape();
}
