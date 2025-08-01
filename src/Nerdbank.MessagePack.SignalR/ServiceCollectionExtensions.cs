// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nerdbank.MessagePack.SignalR;

/// <summary>
/// Extension methods for configuring MessagePack hub protocol.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the MessagePack hub protocol to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the protocol to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMessagePackProtocol(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, MessagePackHubProtocol>());
        return services;
    }

    /// <summary>
    /// Adds the MessagePack hub protocol to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the protocol to.</param>
    /// <param name="serializer">The MessagePack serializer to use.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMessagePackProtocol(this IServiceCollection services, MessagePackSerializer serializer)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, MessagePackHubProtocol>(provider => new MessagePackHubProtocol(serializer)));
        return services;
    }
}