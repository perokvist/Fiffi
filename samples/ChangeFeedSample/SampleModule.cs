using System;
using System.Linq;
using System.Threading.Tasks;
using Fiffi;
using Fiffi.Testing;
using Fiffi.Visualization;
using Microsoft.Extensions.Logging;

namespace ChangeFeedSample
{
    public class SampleModule
    {
        private readonly Dispatcher<ICommand, Task> dispatcher;
        private readonly Func<IEvent[], Task> publish;

        public SampleModule(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish)
        {
            this.dispatcher = dispatcher;
            this.publish = publish;
        }

        public static SampleModule Initialize(IEventStore eventStore, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<SampleModule>();
            var dispatcher = new Dispatcher<ICommand, Task>();
            var ep = new EventProcessor();

            dispatcher.Register<TestCommand>(cmd =>
                ApplicationService.ExecuteAsync<TestState>(
                    eventStore,
                    cmd,
                    state => new[] { new TestEvent(cmd.AggregateId) },
                    events =>
                    {
                        logger.LogInformation($"{nameof(ApplicationService)} published {events.Count()} events.");
                        return Task.CompletedTask;
                    }));

            ep.Register<TestEvent>(e =>
            {
                e.Meta.ForEach(m => logger.LogInformation($"{m.Key} : {m.Value}"));
                return Task.CompletedTask;
            });

            return new SampleModule(dispatcher, events =>
            {
                logger.LogInformation($"{nameof(SampleModule)} got {events.Length} events");
                logger.LogInformation(events.Draw());
                return ep.PublishAsync(events);
            });
        }

        public Task WhenAsync(params IEvent[] events) => this.publish(events);

        public Task DispatchAsync(ICommand command) => this.dispatcher.Dispatch(command);
    }

}

