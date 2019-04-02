using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi
{
	public static class Extensions
	{
		public static T Tap<T>(this T self, Action<T> f)
		{
			f(self);
			return self;
		}

		public static T2 Pipe<T, T2>(this T self, Func<T, T2> f) => f(self);

		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T> action)
		{
			foreach (var item in self)
			{
				action(item);
			}
			return self;
		}

		public static void ForEach<T>(this IEnumerable<T> self, Action<T, int> f)
		{
			var i = 0;
			foreach (var item in self)
			{
				f(item, i);
				i++;
			}
		}

		public static TState Rehydrate<TState>(this IEnumerable<IEvent> events) where TState : new()
			=> events.Aggregate(new TState(), (s, @event) => s.Tap(x => ((dynamic)x).When((dynamic)@event)));

		public static TState Apply<TState>(this IEnumerable<IEvent> events, TState currentState) where TState : new()
			=> events.Aggregate(currentState, (s, @event) => s.Tap(x => ((dynamic)x).When((dynamic)@event)));

		public static void RegisterReceptor<T>(this EventProcessor processor, Dispatcher<ICommand, Task> d, Func<T, ICommand> receptor)
			where T : IEvent
			=> processor.Register<T>(e => d.Dispatch(receptor(e)));

		public static void RegisterReceptor<T>(this EventProcessor processor, Dispatcher<ICommand, Task> d, Func<T, Task<ICommand>> receptor)
			where T : IEvent
			=> processor.Register<T>(async e => await d.Dispatch(await receptor(e)));

		public static void DoIf<T>(this T self, Func<T, bool> p, Action<T> f)
		{
			if (p(self)) f(self);
		}

		public static string AsAggregateName(this string typeName) => typeName.Replace("State", "Aggregate").ToLower();

		public static (string AggregateName, string streamName) AsStreamName(this string typeName, IAggregateId aggregateId) => (typeName.AsAggregateName(), $"{typeName.AsAggregateName()}-{aggregateId.Id}");

	}
}
