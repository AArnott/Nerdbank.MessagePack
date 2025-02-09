// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;

namespace Nerdbank.PolySerializer.Converters;

public class AsyncReader(PipeReader pipeReader)
{
	public PipeReader PipeReader => pipeReader;
}
