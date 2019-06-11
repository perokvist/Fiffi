using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fiffi
{
    public class Dispatcher<TMessage, TResult>
	{
		readonly IDictionary<Type, Func<TMessage, TResult>> _dictionary = new ConcurrentDictionary<Type, Func<TMessage, TResult>>();

		public void Register<T>(Func<T, TResult> func) where T : TMessage
		 => _dictionary.Add(typeof(T), x => func((T)x));

		public TResult Dispatch(TMessage m)
		{
			Func<TMessage, TResult> handler;

			if (_dictionary.TryGetValue(m.GetType(), out handler))
			{
				return handler(m);
			}

			var aggregateMakers = m.GetType().GetTypeInfo().GetInterfaces();

			if (aggregateMakers.Any(aggregateMaker => _dictionary.TryGetValue(aggregateMaker, out handler)))
			{
				return handler(m);
			}

			throw new Exception($"cannot dispatch {m.GetType()}");
		}
	}
}
