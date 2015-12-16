using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fiffi;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Todo.Todo;

namespace Todo
{
	public class Startup
	{
		public IHostingEnvironment Environment { get; }
		public IConfigurationRoot Configuration { get; }
		public Dam Dam { get; }


		public Startup(IHostingEnvironment environment, ILoggerFactory loggerFactory)
		{
			Environment = environment;
			var configurationBuilder = new ConfigurationBuilder();
			configurationBuilder.AddJsonFile($"config.{Environment.EnvironmentName}.json", true);
			configurationBuilder.AddEnvironmentVariables();
			Configuration = configurationBuilder.Build();

			if(environment.IsTest())
				Dam = Dam.CreateDam(() => Fiffi.MessageVault.Stream.Memory(Configuration), loggerFactory);
			if(environment.IsProduction())
				Dam = Dam.CreateDam(() => Fiffi.MessageVault.Stream.Cloud(Configuration), loggerFactory);

			Dam.AddModules((bus, dispatcher, lf) => TodoModule.Initialize(bus));
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddFiffi(Dam);
		}

		public void Configure(IApplicationBuilder app)
		{
			app.UseIISPlatformHandler();

			app.Start(Dam.ListenToStream);

			app.Run(async (context) =>
			{
				await context.Response.WriteAsync("Hello World!");
			});
		}

		public static void Main(string[] args) => WebApplication.Run<Startup>(args);
	}
}
