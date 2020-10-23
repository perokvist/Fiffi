using Fiffi.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi
{
    public static class Policy
    {
        public static T[] Issue<T>(IEvent @event, Func<T[]> f)
        where T : ICommand
            => f()
            .Select(x => Issue(@event, () => x))
            .ToArray();

        public static T Issue<T>(IEvent @event, Func<T> f)
            where T : ICommand
        {
            var cmd = f();
            if (cmd != null)
            {
                //cmd.CommandId = $"{@event.EventId()}-{cmd.GetType()}-{commandIndex}";
                cmd.CorrelationId = @event.GetCorrelation();
                cmd.CausationId = @event.EventId();
            }
            return cmd;
        }

        public static async Task<T> Issue<T>(IEvent @event, Func<Task<T>> f)
           where T : ICommand
        {
            var cmd = await f();
            if (cmd != null)
            {
                //cmd.CommandId = $"{@event.EventId()}-{cmd.GetType()}-{commandIndex}";
                cmd.CorrelationId = @event.GetCorrelation();
                cmd.CausationId = @event.EventId();
            }
            return cmd;
        }

        public static Func<TEvent, PolicyContext, Task> On<TEvent, TProjection>(Func<TEvent, string> streamNameProvider, Func<TEvent, TProjection, ICommand> policy)
            where TEvent : IEvent
            where TProjection : class, new()
        => (e, ctx) => ctx.ExecuteAsync<TProjection>(streamNameProvider(e), p => Issue(e, () => policy(e, p)));

        public static Func<TEvent, PolicyContext, Task> On<TEvent, TProjection>(string streamName, Func<TEvent, TProjection, ICommand> policy)
            where TEvent : IEvent
            where TProjection : class, new()
                => (e, ctx) => ctx.ExecuteAsync<TProjection>(streamName, p => Issue(e, () => policy(e, p)));

        public static Func<TEvent, PolicyContext, Task> On<TEvent, TProjection, TEventFilter>(string streamName, Func<TEvent, TProjection[], IEnumerable<ICommand>> policy)
          where TEvent : IEvent
          where TProjection : class, new()
          where TEventFilter : IEvent
            => On<TEvent, TProjection, TEventFilter>(streamName, (e, p) => policy(e, p).ToArray());

        public static Func<TEvent, PolicyContext, Task> On<TEvent, TProjection, TEventFilter>(string streamName, Func<TEvent, TProjection[], ICommand[]> policy)
            where TEvent : IEvent
            where TProjection : class, new()
            where TEventFilter : IEvent
               => (e, ctx) => ctx.ExecuteAsync<TProjection, TEventFilter>(streamName, p => Issue(e, () => policy(e, p)));

        public static Func<IEvent<TEvent>, PolicyContext, Task> On<TEvent>(Func<IEvent<TEvent>, ICommand> policy)
            where TEvent : EventRecord
            => (e, ctx) => ctx.ExecuteAsync(Issue(e, () => policy(e)));

        public static Func<TEvent, PolicyContext, Task> On<TEvent>(Func<TEvent, ICommand[]> policy)
            where TEvent : IEvent
            => (e, ctx) => ctx
            .ExecuteAsync(
                policy(e)
                .Select(cmd => Issue(e, () => cmd))
                .ToArray());
    }

    public class PolicyContext
    {
        public PolicyContext(Dispatcher<ICommand, Task> dispatcher, IEventStore store)
        {
            Dispatcher = dispatcher;
            Store = store;
        }

        public Dispatcher<ICommand, Task> Dispatcher { get; set; }

        public IEventStore Store { get; set; }

        public Task ExecuteAsync(ICommand cmd) => cmd.DoIfAsync(c => c != null, c => this.Dispatcher.Dispatch(c));

        public async Task ExecuteAsync(ICommand[] cmds)
        {
            foreach (var cmd in cmds)
            {
                await ExecuteAsync(cmd);
            }
        }

        public async Task ExecuteAsync<T>(string streamName, Func<T, ICommand> policy)
            where T : class, new()
        {
            var p = await this.Store.Projector<T>().ProjectAsync(streamName);
            await ExecuteAsync(policy(p));
        }

        public async Task ExecuteAsync<T, TEventFilter>(string streamName, Func<T[], ICommand> policy)
            where T : class, new()
            where TEventFilter : IEvent
        {
            var p = await this.Store.Projector<T>().ProjectAsync<TEventFilter>(streamName);
            await ExecuteAsync(policy(p));
        }

        public async Task ExecuteAsync<T, TEventFilter>(string streamName, Func<T[], ICommand[]> policy)
            where T : class, new()
            where TEventFilter : IEvent
        {
            var p = await this.Store.Projector<T>().ProjectAsync<TEventFilter>(streamName);
            await ExecuteAsync(policy(p));
        }
    }
}
