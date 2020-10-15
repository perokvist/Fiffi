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
        Func<IEvent[], Task> update;
        Func<IEvent[], Func<ICommand, Task>, Task> trigger;
        QueryDispatcher Queries { get; }

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
            update = f;
            return this;
        }

        public SimpleConfiguration<T> Triggers(Func<IEvent[], Func<ICommand, Task>, Task> f)
        {
            trigger = f;
            return this;
        }


        public SimpleConfiguration<T> Query<TQuery, TResult>(Func<TQuery, Task<TResult>> f)
        {
            Queries.Register(f);
            return this;
        }

        public virtual T Create(IEventStore store) => f(dispatch, async events =>
        {
            await update(events);
            await trigger(events, dispatch);
        }, Queries, update);
    }
}
