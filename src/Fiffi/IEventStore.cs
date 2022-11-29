namespace Fiffi;

public interface IEventStore : IEventStore<IEvent>
{
    //public async Task<IEnumerable<IEvent>> LoadEventsByCategory(string categoryName, string allStream = "$all")
    //   => (await LoadEventStreamAsync(allStream, 0)).Events
    //        .Where(x => x.Meta.GetEventMetaData().StreamName.StartsWith(categoryName));
}

public interface IEventStore<T>
{
    Task<long> AppendToStreamAsync(string streamName, long version, params T[] events);

    Task<(IEnumerable<T> Events, long Version)> LoadEventStreamAsync(string streamName, long version);
}
