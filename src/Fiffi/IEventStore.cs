namespace Fiffi;

public interface IEventStore : IEventStore<IEvent>
{
    public async Task<IEnumerable<IEvent>> LoadEventsByCategory(string categoryName, long version, string allStream = "all")
       => (await LoadEventStreamAsync(allStream, version)).Events
            .Where(x => x.Meta.GetEventMetaData().StreamName.StartsWith(categoryName));
}

public interface IEventStore<T>
{
    Task<long> AppendToStreamAsync(string streamName, long version, params T[] events);

    Task<(IEnumerable<T> Events, long Version)> LoadEventStreamAsync(string streamName, long version);
}
