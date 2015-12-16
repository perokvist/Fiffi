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

		public static IEventBus Cloud(IConfiguration configuration)
			=> new MessageVaultEventBus(
				new Client(configuration[$"{MessageVaultConfig}:url"], configuration[$"{MessageVaultConfig}:user"],
					configuration[$"{MessageVaultConfig}:password"]),
				new CloudCheckpointWriter(BlobAccess(configuration[$"{MessageVaultConfig}:storage-connectionstring"])),
					configuration[$"{MessageVaultConfig}:stream-name"],
					Transformation.ToEvent,
					Transformation.ToMessage);

		public static IEventBus Memory(IConfiguration configuration) =>
			new MessageVaultEventBus(new MemoryClient(), new MemoryCheckpointReaderWriter(),
				configuration[$"{MessageVaultConfig}:stream-name"] ?? "test-stream",
				Transformation.ToEvent,
				Transformation.ToMessage);

		private static CloudPageBlob BlobAccess(string connectionString)
		{
			var a = CloudStorageAccount.Parse(connectionString);
			return new CloudPageBlob(a.BlobStorageUri.PrimaryUri, a.Credentials);
		}
	}
}