// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;

#region Sample
public class ChatHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await this.Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async IAsyncEnumerable<string> StreamData(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return $"Data {i}";
            await Task.Delay(1000, cancellationToken);
        }
    }
}
#endregion
