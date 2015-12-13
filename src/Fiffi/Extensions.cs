using System;
using System.Collections.Generic;
using System.Threading;
using MessageVault.Memory;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fiffi
{
	public static class Extensions
	{
		public static void Start(this IApplicationBuilder app, Action<CancellationToken> f)
			=> app.ApplicationServices.GetRequiredService<IApplicationLifetime>()
				.Tap(x => x.ApplicationStarted.Register(() => f(x.ApplicationStopping)));

		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T> f)
		{
			foreach (var item in self)
			{
				f(item);
			}
			return self;
		}

		public static bool IsTest(this IHostingEnvironment environment)
		{
			return environment.IsEnvironment("Test");
		}
					
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