using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Fiffi
{
	public static class Extensions
	{
		public static void Start(this IApplicationBuilder app, Action<CancellationToken> f)
			=> app.ApplicationServices.GetRequiredService<IApplicationLifetime>()
				.Tap(x => x.ApplicationStarted.Register(() => f(x.ApplicationStopping)));

		public static T GetAs<T>(this IEnumerable<KeyValuePair<string, object>> d, string name)
			=> (T) d.Single(x => x.Key == name).Value; //TODO fix types


		public static IEvent With(this IEvent e, string name, object value)
		{
			var m = e.Meta;
			var v = e.Values;

			if (m.ContainsKey(name))
				m = m.SetItem(name, value);
			if (v.ContainsKey(name))
				v = v.SetItem(name, value);

			return e.Create(m, v);
		}

		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T> f)
		{
			foreach (var item in self)
			{
				f(item);
			}
			return self;
		}

		public static bool IsTest(this IHostingEnvironment environment)
			=> environment.IsEnvironment("Test");
					
		public static IServiceCollection AddFiffi(this IServiceCollection services, Dam dam)
		{
			dam.Register(services);
			return services;
		}

		public static T Tap<T>(this T self, Action<T> f)
		{
			f(self);
			return self;
		}
	}
}