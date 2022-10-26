using System.Data;

namespace Fiffi.FileSystem;

public class FileSystemEventStore : IAdvancedEventStore
{
    private readonly string directory;
    private readonly Func<string, Type> typeResolver;
    private readonly Func<DateTime> getHorodate;

    public FileSystemEventStore(string directory, Func<string, Type> typeResolver) : this(directory, typeResolver, () => DateTime.Now) { }

    public FileSystemEventStore(string directory, Func<string, Type> typeResolver, Func<DateTime> getHorodate)
    {
        this.directory = directory;
        this.typeResolver = typeResolver;
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        this.getHorodate = getHorodate;
    }

    public Task<long> AppendToStreamAsync(string streamName, long version, params IEvent[] events)
    {
        var current = GetVersion(streamName);
        if (current != version)
            throw new DBConcurrencyException($"wrong version - expected {version} but was {current}");

        return AppendToStreamAsync(streamName, events);
    }

    public async Task<long> AppendToStreamAsync(string streamName, params IEvent[] events)
    {
        foreach (var item in events)
        {
            var typeKey = item.GetEventName();

            var fileName = $"{streamName}-{ToMsUnixTimeStamp(getHorodate())}-{typeKey}.json";
            var payload = JsonSerializer.Serialize<object>(item);
            await System.IO.File.WriteAllTextAsync(Path.Combine(directory, fileName), payload);
        }

        return GetVersion(streamName);
    }

    private long GetVersion(string streamName)
     => Directory.EnumerateFiles(directory)
        .Where(x => Path.GetFileName(x).StartsWith(streamName))
        .Count();


    private long ToMsUnixTimeStamp(DateTime horodate)
    {
        var unixOriginDate = new DateTime(1970, 1, 1);
        return (long)horodate.Subtract(unixOriginDate).TotalMilliseconds;
    }

    public async Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
    {
        const string eventFilePattern = "(\\d+)\\-(.*).json";
        var readFiles = Directory.EnumerateFiles(directory)
            .Where(x => Path.GetFileName(x).StartsWith(streamName))
            .Where(filePath => Regex.IsMatch(filePath, eventFilePattern))
            .Select(filePath =>
            {
                var match = Regex.Match(Path.GetFileName(filePath), eventFilePattern);
                var horodate = long.Parse(match.Groups[1].Value);
                var eventTypeTag = match.Groups[2].Value;
                return (horodate, filePath, eventType: typeResolver(eventTypeTag));
            })
            .Where(t => t.eventType != default)
            .OrderBy(t => t.horodate)
            .Select(async t =>
            {
                var payload = await File.ReadAllTextAsync(t.filePath);
                var @event = (IEvent)JsonSerializer.Deserialize(payload, t.eventType);
                return @event;
            });

        var events = (await Task.WhenAll(readFiles)).Skip((int)version);

        return (events, version + events.Count());
    }

    public async IAsyncEnumerable<IEvent> LoadEventStreamAsAsync(string streamName, long version)
    {
        var (events, _) = await LoadEventStreamAsync(streamName, version);
        foreach (var e in events)
            yield return e;
    }

    public IAsyncEnumerable<IEvent> LoadEventStreamAsAsync(string streamName, params IStreamFilter[] filters)
    {
        throw new NotImplementedException();
    }
}
