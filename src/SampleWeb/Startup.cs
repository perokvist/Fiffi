using System.Threading.Tasks;
using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Data;
using SampleWeb.Cart;

namespace SampleWeb
{
	public class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			var deserializer = Serialization.JsonDeserialization(TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(ItemAddedEvent))));
			services.AddSingleton(sc => CartModule.Initialize(sc.GetService<IReliableStateManager>(), events => Task.CompletedTask));
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sc => new Publisher(sc.GetService<IReliableStateManager>(), deserializer));
			services.AddMvc();
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseMvc();

			app.Run(async (context) =>
			{
				await context.Response.WriteAsync("Hello From SamleWeb!");
			});
		}
	}
}
