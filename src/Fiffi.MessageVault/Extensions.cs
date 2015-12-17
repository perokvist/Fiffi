using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MessageVault;
using MessageVault.Api;
using Microsoft.Extensions.Logging;

namespace Fiffi.MessageVault
{
	public static class Extensions
	{
		public static Action<CancellationToken, string, Func<IEnumerable<MessageWithId>, Task>> CreateConsumer(
			this IClient client, ILogger l, ICheckpointWriter checkpoint) =>
			(ct, streamName, f) =>
			{
				Task.Factory.StartNew(async () =>
				{
					var current = checkpoint.GetOrInitPosition();
					var reader = await client.GetMessageReaderAsync(streamName);

					while (!ct.IsCancellationRequested)
					{
						l.LogDebug("Fetching");

						var result = await reader.GetMessagesAsync(ct, current, 100);
						if (result.HasMessages())
							await f(result.Messages);

						current = result.NextOffset;
						checkpoint.Update(current);
					}
				}, TaskCreationOptions.LongRunning);
			};
	}
}