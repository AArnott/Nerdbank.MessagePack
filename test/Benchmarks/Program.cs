// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

BenchmarkSwitcher switcher = new(typeof(Program).Assembly);

#if DEBUG
switcher.Run(args, new DebugInProcessConfig());
#else
switcher.Run(args);
#endif
