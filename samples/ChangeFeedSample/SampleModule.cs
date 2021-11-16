using System;
using System.Linq;
using System.Threading.Tasks;
using Fiffi;
using Fiffi.Modularization;
using Fiffi.Testing;
using Fiffi.Visualization;
using Microsoft.Extensions.Logging;

namespace ChangeFeedSample;

public class SampleModule : Module
{
    public SampleModule(Func<ICommand, Task> dispatcher, Func<IEvent[], Task> publish) : base(dispatcher, publish, new())
    { }

    public static SampleModule Initialize(IEventStore eventStore, ILoggerFactory loggerFactory)
        => new Configuration<SampleModule>((d, pub, q, start) => new SampleModule(d, events =>
        {
            var logger = loggerFactory.CreateLogger<SampleModule>();
            logger.LogInformation($"{nameof(SampleModule)} got {events.Length} events");
            logger.LogInformation(events.Draw());
            return pub(events);
        }))
        .Commands(cmd => ApplicationService.ExecuteAsync<TestState>(
                eventStore,
                cmd,
                state => new[] { new TestEventRecord("test") },
                events =>
                {
                    var logger = loggerFactory.CreateLogger<SampleModule>();
                    logger.LogInformation($"{nameof(ApplicationService)} published {events.Count()} events.");
                    return Task.CompletedTask;
                }))
        .Updates(events =>
        {
            var logger = loggerFactory.CreateLogger<SampleModule>();
            foreach (var e in events)
            {
                e.Meta.ForEach(m => logger.LogInformation($"{m.Key} : {m.Value}"));
            }
            return Task.CompletedTask;

        })
        .Create(eventStore);
}

