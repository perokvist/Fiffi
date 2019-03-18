using Fiffi;
using Microsoft.Azure.ServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;

public class ServiceBusEventCommunication : IEventCommunication
{
	readonly SubscriptionClient subscriptionClient;
	readonly TopicClient topicClient;

	public ServiceBusEventCommunication(TopicClient topicClient,  SubscriptionClient subscriptionClient)
	{
		this.topicClient = topicClient;
		this.subscriptionClient = subscriptionClient;
	}

	public async Task OnShutdownAsync()
	{
		if (!this.topicClient.IsClosedOrClosing)
			await this.topicClient.CloseAsync();

		if (!this.subscriptionClient.IsClosedOrClosing)
			await this.subscriptionClient.CloseAsync();
	}

	public Task PublichAsync(IEvent @event)
	 => this.topicClient.SendAsync((Message)@event); //TODO Deserialization

	public Task SubscribeAsync(Func<IEvent, CancellationToken, Task> onEvent)
	{
		this.subscriptionClient.RegisterMessageHandler((m, ct) => onEvent((IEvent)m, ct), new MessageHandlerOptions(null)); //TODO serialization
		return Task.CompletedTask;
	}
}
