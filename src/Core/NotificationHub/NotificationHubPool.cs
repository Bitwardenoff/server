﻿using Bit.Core.Settings;
using Bit.Core.Utilities;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Logging;

namespace Bit.Core.NotificationHub;

public class NotificationHubPool : INotificationHubPool
{
    private List<NotificationHubConnection> _connections { get; }
    private readonly IEnumerable<INotificationHubClient> _clients;
    private readonly ILogger<NotificationHubPool> _logger;
    public NotificationHubPool(ILogger<NotificationHubPool> logger, GlobalSettings globalSettings)
    {
        _logger = logger;
        _connections = FilterInvalidHubs(globalSettings.NotificationHubPool.NotificationHubs);
        _clients = _connections.GroupBy(c => c.ConnectionString).Select(g => g.First().HubClient);
    }

    private List<NotificationHubConnection> FilterInvalidHubs(IEnumerable<GlobalSettings.NotificationHubSettings> hubs)
    {
        List<NotificationHubConnection> result = new();
        foreach (var hub in hubs)
        {
            var connection = NotificationHubConnection.From(hub);
            if (!connection.IsValid)
            {
                _logger.LogWarning("Invalid notification hub settings: {HubName}", hub.HubName ?? "hub name missing");
                continue;
            }
            result.Add(connection);
        }

        return result;
    }


    /// <summary>
    /// Gets the NotificationHubClient for the given comb ID.
    /// </summary>
    /// <param name="comb"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown when no notification hub is found for a given comb.</exception>
    public NotificationHubClient ClientFor(Guid comb, ApplicationChannel channel)
    {
        var possibleConnections = _connections.Where(c => c.RegistrationEnabled(comb, channel)).ToArray();
        if (possibleConnections.Length == 0)
        {
            throw new InvalidOperationException($"No valid notification hubs are available for the given comb ({comb}) and channel ({channel}).\n" +
                $"The comb's datetime is {CoreHelpers.DateFromComb(comb)}." +
                $"Hub start and end times are configured as follows:\n" +
                string.Join("\n", _connections.Select(c => $"Hub {c.HubName} (channel: {c.Channel}) - Start: {c.RegistrationStartDate}, End: {c.RegistrationEndDate}")));
        }
        return possibleConnections[CoreHelpers.BinForComb(comb, possibleConnections.Length)].HubClient;
    }

    public INotificationHubProxy AllClients { get { return new NotificationHubClientProxy(_clients); } }
}
