# Fiffi
Exploration kit for eventdriven services.



### Dam Creation ASP.NET 5

			Dam = Dam.CreateMemoryDam(Configuration, loggerFactory);

#### Module registeration

All modules created will be registered as thier type on the service colleciton, in service registration.
This gives you the flexibility to handle each module as you see fit.

			Dam.AddModules((bus, dispatcher, lf) => TodoModule.Initialize(bus));

#### Register Services


	    public void ConfigureServices(IServiceCollection services)
	    {
		    services.AddFiffi(Dam);
	    }


#### Start listening to stream

        public void Configure(IApplicationBuilder app)
        {
            app.UseIISPlatformHandler();

			app.Start(Dam.ListenToStream);

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }


### Modules

Each module could handle/store state/views. The module could apply changes to state and publish to the stream using 2PC, or publishing to the steam and mutate state by subscribing to the stream.
The option to lock on instance is provided by the ApplicationService util, and the EventProcessor util.