using System;
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
		private IEventBus pub;

		private ServerFixture(Action<IApplicationBuilder> a, Action<IServiceCollection> a2)
		{
			//var env = new HostingEnvironment
			//{
			//	EnvironmentName = "Test"
			//};
			//var startup = new Startup(env);
			var lf = new LoggerFactory { MinimumLevel = LogLevel.Debug };
			lf.AddConsole(LogLevel.Verbose);


			server = TestServer.Create(a, collection =>
			{
				a2(collection);
				pub = (IEventBus)collection.Single(x => x.ServiceType == typeof(IEventBus)).ImplementationInstance;
			});
			
			client = server.CreateClient();
		}

		public static async Task UseAsync(
			Action<IApplicationBuilder> a, 
			Action<IServiceCollection> a2, 
			Func<HttpClient, IEventBus, Task> f)
		{
			using (var s = new ServerFixture(a, a2))
			{
				await f(s.client, s.pub);
			}
		}

		public void Dispose()
		{
			server.Dispose();
			client.Dispose();
		}
	}
}