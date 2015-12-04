using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fiffi
{
	public static class Extensions
	{

		public static IApplicationBuilder InitializeEventBus(this IApplicationBuilder app)
		{
			var pub = app.ApplicationServices.GetService<IEventBus>();

			pub.Run(app.ApplicationServices
				.GetRequiredService<IApplicationLifetime>()
				.ApplicationStopping,
				app.ApplicationServices.GetService<ILogger<IEventBus>>());

			return app;
		}


		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T> f)
		{
			foreach (var item in self)
			{
				f(item);
			}
			return self;
		} 
	}
}