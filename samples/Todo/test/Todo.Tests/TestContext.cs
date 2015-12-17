using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fiffi;
using Fiffi.Testing;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Logging;

namespace Todo.Tests
{
	// This project can output the Class library as a NuGet Package.
	// To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
	public class TestContext 
	{
		private readonly Env.Context context;

		public TestContext()
		{
			var s = new Startup(new HostingEnvironment() { EnvironmentName = "Test" }, new LoggerFactory());
			context = Env.CreateContext(builder => s.Configure(builder),
				collection => s.ConfigureServices(collection));
		}

		public Task RunAsync(Func<HttpClient, Dam, Task> @case)
			=> context.RunAsync(@case);

		public Task RunAsync(Env.UseCaseData @case)
			=> context.RunAsync(@case);

		public void Dispose() => context.Dispose();
	}
}
