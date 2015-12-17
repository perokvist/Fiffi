using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageVault;
using MessageVault.Api;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Fiffi.MessageVault
{
	public class EventBus : IEventBus
	{
		private readonly IClient _client;
		private readonly ICheckpointWriter _checkpoint;
		private readonly string _streamName;
		private readonly Func<IEnumerable<MessageWithId>, IEvent[]> _t;
		private readonly Func<IEvent, Message> _toMessage;
		private readonly List<Func<IEvent[], Task>> _processors = new List<Func<IEvent[], Task>>();

		public EventBus(
			IClient client,
			ICheckpointWriter checkpoint,
			string streamName,
			Func<IEnumerable<MessageWithId>, IEvent[]> t, 
			Func<IEvent, Message> toMessage)
		{
			_client = client;
			_checkpoint = checkpoint;
			_streamName = streamName;
			_t = t;
			_toMessage = toMessage;
		}

		public void Run(CancellationToken ct, ILogger l)
			=> _client.CreateConsumer(l, _checkpoint)
			(ct, _streamName, events => Task.WhenAll(_processors.Select(x => x(_t(events)))));


	public void Subscribe(Func<IEvent[], Task> processor)
		=> _processors.Add(processor);

	public void Subscribe<T>(Func<T, Task> f) where T : IEvent
		=> _processors.Add(events => Task.WhenAll(events.Select(e => f((T)e)))); //TODO applicable - casting ?

		public async Task PublishAsync(params IEvent[] events)
		{
			if (events.Any())
				await _client.PostMessagesAsync(_streamName, events.Select(_toMessage).ToArray());
		}
	}
}