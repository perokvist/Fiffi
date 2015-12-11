using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageVault;
using MessageVault.Api;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Fiffi
{
	public class MessageVaultEventBus : IEventBus
	{
		private readonly IClient _client;
		private readonly ICheckpointWriter _checkpoint;
		private readonly string _streamName;
		private readonly List<Func<IEvent[], Task>> _processors = new List<Func<IEvent[], Task>>();

		public MessageVaultEventBus(
			IClient client,
			ICheckpointWriter checkpoint,
			string streamName)
		{
			_client = client;
			_checkpoint = checkpoint;
			_streamName = streamName;
		}

		public void Run(CancellationToken ct, ILogger l)
		{
			Task.Factory.StartNew<Task>(async () =>
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
							.Select(Transformation.ToEvent)
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
		//TODO envelope - maps
		var messages = events
			.Select(Transformation.ToMessage)
			.ToList();

		if (!messages.Any())
		{
			return;
		}

		await _client.PostMessagesAsync(_streamName, messages);
	}
}
}