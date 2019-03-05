using System;
using System.Threading.Tasks;
using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace SampleWeb
{
	public class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<MailboxOptions>(opt =>
			{
				opt.Serializer = Serialization.Json();
				opt.Deserializer = Serialization.JsonDeserialization(TypeResolver.Default());
			});
			services.AddCart();
			services.AddMailboxes(sp => new Func<IEvent, Task>[] { sp.GetRequiredService<CartModule>().WhenAsync });

			services
				.AddMvc()
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
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
				await context.Response.WriteAsync("Hello From SampleWeb!");
			});
		}
	}
}
