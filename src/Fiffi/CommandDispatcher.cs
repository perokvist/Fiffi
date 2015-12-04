using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi
{

	public class Dispatcher<TMessage, TResult>
	{
		private readonly Dictionary<Type, Func<TMessage, TResult>> _dictionary = new Dictionary<Type, Func<TMessage, TResult>>();

		public void Register<T>(Func<T, TResult> func) where T : TMessage
		{
			_dictionary.Add(typeof(T), x => func((T)x));
		}

		public TResult Dispatch(TMessage m)
		{
			Func<TMessage, TResult> handler;

			if (_dictionary.TryGetValue(m.GetType(), out handler))
			{
				return handler(m);
			}

			var aggregateMakers = m.GetType().GetInterfaces()
			   .Where(x => !(x == typeof(ICommand)));

			if (aggregateMakers.Any(aggregateMaker => _dictionary.TryGetValue(aggregateMaker, out handler)))
			{
				return handler(m);
			}

			throw new Exception("cannot dispatch " + m.GetType());
		}
	}

	public class CommandDispatcher : Dispatcher<ICommand, Task>	//TODO remove
	{}
}