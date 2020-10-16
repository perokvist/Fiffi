using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi.Modularization
{
    public class SimpleConfiguration<T>
        where T : Module
    {
        readonly Func<Func<ICommand, Task>, Func<IEvent[], Task>, QueryDispatcher, Func<IEvent[], Task>, T> f;

        public SimpleConfiguration(Func<Func<ICommand, Task>, Func<IEvent[], Task>, QueryDispatcher, Func<IEvent[], Task>, T> f)
        {
            this.f = f;
        }

        Func<ICommand, Task> dispatch;
        List<Func<IEvent[], Task>> updates = new List<Func<IEvent[], Task>>();
        List<Func<IEvent[], Func<ICommand, Task>, Task>> triggers = new List<Func<IEvent[], Func<ICommand, Task>, Task>>();
        QueryDispatcher queries = new QueryDispatcher();

        public SimpleConfiguration<T> Commands(params Func<ICommand, Task>[] f)
        {
            dispatch = f.Aggregate((l, r) => async c =>
               {
                   await l(c);
                   await r(c);
               });
            return this;
        }

        public SimpleConfiguration<T> Updates(Func<IEvent[], Task> f)
        {
            updates.Add(f);
            return this;
        }

        public SimpleConfiguration<T> Triggers(Func<IEvent[], Func<ICommand, Task>, Task> f)
        {
            triggers.Add(f);
            return this;
        }


        public SimpleConfiguration<T> Query<TQuery, TResult>(Func<TQuery, Task<TResult>> f)
        {
            queries.Register(f);
            return this;
        }

        public virtual T Create(IEventStore store) => f(dispatch, async events =>
        {
            await Task.WhenAll(updates.Select(x => x(events)));
            await Task.WhenAll(triggers.Select(t => t(events, dispatch)));
        }, queries, x => Task.WhenAll(updates.Select(u => u(x))));
    }
}
