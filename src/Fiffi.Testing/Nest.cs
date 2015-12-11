using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi.Testing
{
	public class Nest : IDisposable
	{

		public static async Task<Nest> InitializeAsync(IEventBus pub, IEvent[] given)
		{
			var nest = new Nest();

			await pub.PublishAsync(given);

			pub.Register<IEvent>(@event =>
			{
				nest.Happend.Add(@event);
				return Task.FromResult(0);
			});

			return nest;
		}

		public List<IEvent> Happend = new List<IEvent>(); 

		public void Dispose(){}	 // clear list ? :)
	}
}