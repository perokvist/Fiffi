using Fiffi.InMemory;
using Fiffi.Modularization;
using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests
{
    public class TestContextTests
    {
        [Fact]
        public async Task ContextPublishesToMainModuleAsync()
        {
            var context = TestContextBuilder.Create<InMemoryEventStore, TestModule>(TestModule.Initialize, OtherModule.Initialize);
            var id = new AggregateId(Guid.NewGuid());
            await context.WhenAsync(new TestCommand(id));

            context.Then(events => events.OfType<TestEvent>().Happened());
        }

        [Fact]
        public async Task ContextPublishesToAdditionalModuleAsync()
        {
            var context = TestContextBuilder.Create<InMemoryEventStore, TestModule>(TestModule.Initialize, OtherModule.Initialize);
            var id = new AggregateId(Guid.NewGuid());
            await context.WhenAsync(new TestCommand(id));

            context.Then(events => Assert.True(events.OfType<OtherEvent>().Happened()));
        }

        public class TestModule : Module
        {
            public TestModule(Func<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher,
                Func<IEvent[], Task> onStart) : base(dispatcher, publish, queryDispatcher, onStart)
            { }

            public static TestModule Initialize(IEventStore store, Func<IEvent[], Task> pub)
                => new ModuleConfiguration<TestModule>((d, p, q, s) => new TestModule(d, p, q, s))
                .Command<TestCommand>(cmd => pub(new[] { new TestEvent().AddTestMetaData<string>(cmd.AggregateId) }))
                .Create(store);
        }

        public class OtherModule : Module
        {
            public OtherModule(Func<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher,
                Func<IEvent[], Task> onStart) : base(dispatcher, publish, queryDispatcher, onStart)
            { }

            public static OtherModule Initialize(IEventStore store, Func<IEvent[], Task> pub)
                => new ModuleConfiguration<OtherModule>((d, p, q, s) => new OtherModule(d, p, q, s))
                .Projection<TestEvent>(e => pub(new[] { new OtherEvent().AddTestMetaData<string>(new AggregateId(e.SourceId)) }))
                .Create(store);
        }

        public class OtherEvent : TestEvent
        { }
    }
}
