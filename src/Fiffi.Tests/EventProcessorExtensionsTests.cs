using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests
{
    public class EventProcessorExtensionsTests
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task DoWhenThenAsync(bool when, bool expectedThenCalled)
        {
            var doCalled = false;
            var thenCalled = false;
            var thenCalledLast = true;
            var ep = new EventProcessor();

            ep
            .Always<TestEvent>(e =>
            {
                doCalled = true;
                thenCalledLast = false;
                return Task.CompletedTask;
            })
            .When(p => when)
            .Then(e =>
            {
                thenCalled = true;
                thenCalledLast = true;
                return Task.CompletedTask;
            });

            await ep.PublishAsync(new AggregateId("b").Pipe(id => new TestEvent(id).AddTestMetaData<string>(id)));

            Assert.True(doCalled, "Do not called");
            Assert.Equal(expectedThenCalled, thenCalled);
            if (when) Assert.True(thenCalledLast);

        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task RegisterReceptorWithAsync(bool letThrough, bool expectdispatch)
        {
            //Arrange
            var ep = new EventProcessor();
            var d = new Dispatcher<ICommand, Task>();
            var dispatched = false;

            d.Register<TestCommand>(c =>
            {
                dispatched = true;
                return Task.CompletedTask;
            });

            ep
            .RegisterReceptorWith<TestEvent>(d, e => Policy.Issue(e, () => new TestCommand(e.SourceId)))
            .Guard(e => letThrough);

            //Act
            await ep.PublishAsync(new AggregateId("t").Pipe(id => new TestEvent(id).AddTestMetaData<string>(id)));

            //Assert
            Assert.Equal(expectdispatch, dispatched);
        }


        [Theory]
        [InlineData(EventContext.Inbox, true)]
        [InlineData(EventContext.Replay, false)]
        [InlineData(EventContext.InProcess, false)]
        public async Task ContextWithConditionAsync(EventContext context, bool expected)
        {
            //Arrange
            var ep = new EventProcessor<EventContext>();
            var published = false;

            ep
             .Register<TestEvent, EventContext>()
             .When((e, c) => c == context)
             .Then((e, c) =>
             {
                 published = true;
                 return Task.CompletedTask;
             });

            //Act
            await ep.PublishAsync(
                EventContext.Inbox,
                new AggregateId("t").Pipe(id => new TestEvent(id).AddTestMetaData<string>(id)));

            //Assert
            Assert.Equal(expected, published);
        }

        [Fact]
        public async Task WithContextAsync()
        {
            //Arrange
            var published = false;
            var ep = new EventProcessor<EventContext>();

            ep.Register<IEvent>((e, c) =>
            {
                published = true;
                return Task.CompletedTask;
            });

            //Act
            await ep.PublishAsync(EventContext.Inbox, new AggregateId("t").Pipe(id => new TestEvent(id).AddTestMetaData<string>(id)));

            //Assert
            Assert.True(published);
        }

        [Fact]
        public async Task WithConextConditionalAsync()
        {
            //Arrange
            var ep = new EventProcessor<EventContext>();
            var published = false;

            ep
             .Register<IEvent, EventContext>()
             .When((e, c) => e.Is<TestEvent>())
             .Then((e, c) =>
             {
                 published = true;
                 return Task.CompletedTask;
             });

            //Act
            await ep.PublishAsync(
                EventContext.Inbox,
                new AggregateId("t")
                .Pipe(id => new TestEvent(id).AddTestMetaData<string>(id)));

            //Assert
            Assert.True(published);
        }
    }
}
