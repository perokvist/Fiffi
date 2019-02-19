using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ServiceFabric.Mocks;
using Xunit;
using System.Threading.Tasks;

namespace SampleWeb.Tests
{
	public class WebTests
	{
		private HttpClient client;

		public WebTests()
		{
			var server = new TestServer(
				new WebHostBuilder()
				.UseEnvironment("Development")
				.ConfigureServices(services => services.AddSingleton<IReliableStateManager>(new MockReliableStateManager()))
				.UseStartup<Startup>());

			this.client = server.CreateClient();
		}

		[Fact]
		public async Task HelloAsync()
		{
			var req = new HttpRequestMessage(new HttpMethod("GET"), "/");

			var res = await client.SendAsync(req);

			res.EnsureSuccessStatusCode();
			Assert.Contains("Hello", await res.Content.ReadAsStringAsync());
		}
	}
}
