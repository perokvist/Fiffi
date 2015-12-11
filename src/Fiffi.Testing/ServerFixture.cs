﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fiffi.Testing
{

	public class ServerFixture : IDisposable
	{
		private TestServer server;
		private HttpClient client;
		private Lodge lodge;

		private ServerFixture(Action<IApplicationBuilder> a, Action<IServiceCollection> a2)
		{
			var lf = new LoggerFactory { MinimumLevel = LogLevel.Debug };
			lf.AddConsole(LogLevel.Verbose);
			server = TestServer.Create(a, collection =>
			{
				a2(collection);
				//TODO get from startup as property ? or via create as param
				lodge = (Lodge) collection.Single(x => x.ServiceType == typeof (Lodge)).ImplementationInstance;
			});
			client = server.CreateClient();
		}

		internal static ServerFixture Create(Action<IApplicationBuilder> a, Action<IServiceCollection> a2)	
			=> new ServerFixture(a, a2);

		public Task RunAsync(Func<HttpClient, Lodge, Task> @case)
			=> @case(client, lodge);

		public static async Task UseAsync(
			Action<IApplicationBuilder> a, 
			Action<IServiceCollection> a2, 
			Func<HttpClient, Lodge, Task> f)
		{
			using (var s = new ServerFixture(a, a2))
			{
				await f(s.client, s.lodge);
			}
		}

		public void Dispose()
		{
			server.Dispose();
			client.Dispose();
		}
	}
}