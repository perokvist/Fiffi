using Fiffi.Projections;
using System;
using System.Threading.Tasks;

namespace Fiffi
{
    public static class Policy
    {
        public static T Issue<T>(IEvent @event, Func<T> f)
            where T : ICommand
        {
            var cmd = f();
            if (cmd != null)
            {
                cmd.CorrelationId = @event.GetCorrelation();
                cmd.CausationId = @event.EventId();
            }
            return cmd;
        }

        public static Func<TEvent, PolicyContext, Task> On<TEvent, TProjection>(Func<TEvent, string> streamNameProvider, Func<TEvent, TProjection, ICommand> policy)
        where TProjection : class, new()
        => (e, ctx) => ctx.ExecuteAsync<TProjection>(streamNameProvider(e), p => policy(e, p));

        public static Func<TEvent, PolicyContext, Task> On<TEvent, TProjection>(string streamName, Func<TEvent, TProjection, ICommand> policy)
            where TProjection : class, new()
            => (e, ctx) => ctx.ExecuteAsync<TProjection>(streamName, p => policy(e, p));

        public static Func<TEvent, PolicyContext, Task> On<TEvent>(Func<TEvent, ICommand> policy)
            => (e, ctx) => ctx.ExecuteAsync(policy(e));

        public static Func<TEvent, PolicyContext, Task> On<TEvent>(Func<TEvent, ICommand[]> policy)
            where TEvent : IEvent
            => async (e, ctx) =>
            {
                foreach (var cmd in policy(e))
                    await ctx.ExecuteAsync(Issue(e, () => cmd));
            };
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

        public async Task ExecuteAsync<T>(string streamName, Func<T, ICommand> policy)
            where T : class, new()
        {
            var p = await this.Store.Projector<T>().ProjectAsync(streamName);
            await policy(p).DoIfAsync(cmd => cmd != null, cmd => this.Dispatcher.Dispatch(cmd));
        }
    }
}
