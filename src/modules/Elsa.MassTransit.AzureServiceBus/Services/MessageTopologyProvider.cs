using Elsa.MassTransit.AzureServiceBus.Models;

namespace Elsa.MassTransit.AzureServiceBus.Services;

/// <summary>
/// Provides message topology information.
/// </summary>
public class MessageTopologyProvider
{
    private readonly IEnumerable<MessageSubscriptionTopology> _subscriptionTopology;

    /// <summary>
    /// Provides message topology information.
    /// </summary>
    public MessageTopologyProvider(IEnumerable<MessageSubscriptionTopology> subscriptionTopology)
    {
        _subscriptionTopology = subscriptionTopology;
    }

    /// <summary>
    /// Retrieves all the short-lived message subscriptions from the subscription topology.
    /// </summary>
    public IEnumerable<MessageSubscriptionTopology> GetShortLivedSubscriptions()
    {
        return _subscriptionTopology.Where(x => x.IsShortLived);
    }
}