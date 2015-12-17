using System;
using System.Linq;
using System.Collections.Generic;
using MessageVault;
using MessageVault.Api;
using MessageVault.Cloud;
using MessageVault.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Fiffi.MessageVault
{
	public static class Stream
	{
		private const string MessageVaultConfig = "fiffi:messagevault";

		public static IEventBus Cloud(IConfiguration configuration) =>
			Cloud(configuration, Events.GetEventTypes());

		public static IEventBus Cloud(IConfiguration configuration, IDictionary<string, Type> eventTypes)
			=> new EventBus(
				new Client(configuration[$"{MessageVaultConfig}:url"], configuration[$"{MessageVaultConfig}:user"],
					configuration[$"{MessageVaultConfig}:password"]),
				new CloudCheckpointWriter(BlobAccess(configuration[$"{MessageVaultConfig}:storage-connectionstring"])),
					configuration[$"{MessageVaultConfig}:stream-name"],
				messages => MessageHandler(messages, eventTypes),
				Transformation.ToMessage);

		public static IEventBus Memory(IConfiguration configuration) =>
			Memory(configuration, Events.GetEventTypes());

		public static IEventBus Memory(IConfiguration configuration, IDictionary<string, Type> eventTypes)
			=> new EventBus(new MemoryClient(), new MemoryCheckpointReaderWriter(),
				configuration[$"{MessageVaultConfig}:stream-name"] ?? "test-stream",
				messages => MessageHandler(messages, eventTypes),
				Transformation.ToMessage);

		private static IEvent[] MessageHandler(IEnumerable<MessageWithId> m, IDictionary<string, Type> d)
			=> m.Where(x => KnownEvent(d, x))
				.Select(x => Transformation.ToEvent(x, TypeFromMessage(d, x)))
			.ToArray();

		private static Type TypeFromMessage(IDictionary<string, Type> et, MessageWithId m) =>
			et[Transformation.Key.FromString(m.KeyAsString()).EventName];

		private static bool KnownEvent(IDictionary<string, Type> et, MessageWithId m) =>
			et.ContainsKey(Transformation.Key.FromString(m.KeyAsString()).EventName);

		private static CloudPageBlob BlobAccess(string connectionString)
		{
			var a = CloudStorageAccount.Parse(connectionString);
			return new CloudPageBlob(a.BlobStorageUri.PrimaryUri, a.Credentials);
		}
	}
}