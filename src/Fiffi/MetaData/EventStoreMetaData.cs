namespace Fiffi
{
    public record EventStoreMetaData
	{
		public long EventVersion { get; set; }
		public long EventPosition { get; set; }
	}
}
