// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nerdbank.MessagePack.SignalR;

/// <summary>
/// Extension methods for configuring MessagePack hub protocol.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <inheritdoc cref="AddMessagePackProtocol{TBuilder}(TBuilder, ITypeShapeProvider, MessagePackSerializer)"/>
	public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder, ITypeShapeProvider typeShapeProvider)
		where TBuilder : ISignalRBuilder
	{
		builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, MessagePackHubProtocol>(provider => new MessagePackHubProtocol(typeShapeProvider)));
		return builder;
	}

	/// <summary>
	/// Adds the MessagePack hub protocol to the specified <see cref="IServiceCollection"/>,
	/// implemented by Nerdbank.MessagePack.
	/// </summary>
	/// <typeparam name="TBuilder">The type of the builder.</typeparam>
	/// <param name="builder">The builder to add the protocol to.</param>
	/// <param name="typeShapeProvider"><inheritdoc cref="MessagePackHubProtocol(ITypeShapeProvider, MessagePackSerializer)" path="/param[@name='typeShapeProvider']"/></param>
	/// <param name="serializer"><inheritdoc cref="MessagePackHubProtocol(ITypeShapeProvider, MessagePackSerializer)" path="/param[@name='serializer']"/></param>
	/// <returns>The builder so that additional calls can be chained.</returns>
	public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder, ITypeShapeProvider typeShapeProvider, MessagePackSerializer? serializer)
		where TBuilder : ISignalRBuilder
	{
		builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, MessagePackHubProtocol>(provider => new MessagePackHubProtocol(typeShapeProvider, serializer)));
		return builder;
	}
}
