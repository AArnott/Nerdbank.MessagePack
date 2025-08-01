// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;

internal class MockInvocationBinder : IInvocationBinder
{
	internal Dictionary<string, IReadOnlyList<Type>> ParameterTypes { get; } = new(StringComparer.Ordinal);

	internal Dictionary<string, Type> ReturnType { get; } = new(StringComparer.Ordinal);

	internal Dictionary<string, Type> StreamItemType { get; } = new(StringComparer.Ordinal);

	public IReadOnlyList<Type> GetParameterTypes(string methodName) => this.ParameterTypes[methodName];

	public Type GetReturnType(string invocationId) => this.ReturnType[invocationId];

	public Type GetStreamItemType(string streamId) => this.StreamItemType[streamId];
}
