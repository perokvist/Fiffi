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
	public class MessageVaultEventBus : IEventBus
	{
		private readonly IClient _client;
		private readonly ICheckpointWriter _checkpoint;
		private readonly string _streamName;
		private readonly Func<MessageWithId, IEvent> _toEvent;
		private readonly Func<IEvent, Message> _toMessage;
		private readonly List<Func<IEvent[], Task>> _processors = new List<Func<IEvent[], Task>>();

		public MessageVaultEventBus(
			IClient client,
			ICheckpointWriter checkpoint,
			string streamName,
			Func<MessageWithId, IEvent> toEvent,
			Func<IEvent, Message> toMessage)
		{
			_client = client;
			_checkpoint = checkpoint;
			_streamName = streamName;
			_toEvent = toEvent;
			_toMessage = toMessage;
		}

		public void Run(CancellationToken ct, ILogger l)
		{
			Task.Factory.StartNew(async () =>
			{
				var current = _checkpoint.GetOrInitPosition();
				var reader = await _client.GetMessageReaderAsync(_streamName);

				while (!ct.IsCancellationRequested)
				{
					l.LogDebug("Fetching");

					var result = await reader.GetMessagesAsync(ct, current, 100);
					if (result.HasMessages())
					{
						var m = result.Messages
							.Select(_toEvent)
							.ToArray();

						var h = _processors.Select(p => p(m));
						await Task.WhenAll(h);
					}
					current = result.NextOffset;
					_checkpoint.Update(current);
				}
			}, TaskCreationOptions.LongRunning);

		}

	public void Register(Func<IEvent[], Task> processor)
	{
		_processors.Add(processor);
	}

	public void Register<T>(Func<T, Task> f)
		where T : IEvent
	{
		_processors.Add(events => Task.WhenAll(events.Select(e => f((T)e)))); //TODO applicable - casting ?
	}

	public async Task PublishAsync(params IEvent[] events)
	{
		var messages = events
			.Select(_toMessage)
			.ToList();

		if (!messages.Any())
			return;

		await _client.PostMessagesAsync(_streamName, messages);
	}
}
}