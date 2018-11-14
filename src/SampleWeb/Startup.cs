﻿using System;
using System.Collections.Generic;
using System.Linq;
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
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			var deserializer = Serialization.JsonDeserialization(TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(ItemAddedEvent))));
			services.AddSingleton(sc =>
				CartModule.Initialize(sc.GetService<IReliableStateManager>(), tx =>
					new ReliableEventStore(
						sc.GetService<IReliableStateManager>(),
						tx,
						Serialization.Json(),
						deserializer
				 	), events => Task.CompletedTask));
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sc => new Publisher(sc.GetService<IReliableStateManager>(), deserializer));
			services.AddMvc();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
