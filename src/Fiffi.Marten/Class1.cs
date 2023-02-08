using Marten;

namespace Fiffi.Marten;
public class EventStore : IEventStore<EventData>
{
    readonly IDocumentStore store;

    public EventStore(IDocumentStore store)
    {
        this.store = store;
    }
    public async Task<long> AppendToStreamAsync(string streamName, long version, params EventData[] events)
    {
        await using var session = store.OpenSession();
        var a = session.Events.Append(streamName, version, events);
        await session.SaveChangesAsync();
        return a.Version;
    }

    async Task<(IEnumerable<EventData> Events, long Version)> IEventStore<EventData>.LoadEventStreamAsync(string streamName, long version)
    {
        using var session = store.LightweightSession();
        //var aggregate = await session.Events.LoadAsync<EventData>(streamName, version);
        //return aggregate ?? throw new InvalidOperationException($"No aggregate by id {id}.");
        throw new NotImplementedException();
    }
}
