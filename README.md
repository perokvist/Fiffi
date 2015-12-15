# Fiffi
Exploration kit for eventdriven services.



### Dam Creation ASP.NET 5

			if(environment.IsTest())
				Dam = Dam.CreateDam(() => Fiffi.Streams.MessageVault.Memory(Configuration), loggerFactory);
			if(environment.IsProduction())
				Dam = Dam.CreateDam(() => Fiffi.Streams.MessageVault.Cloud(Configuration), loggerFactory);


#### Module registeration

All modules created will be registered as thier type on the service colleciton, in service registration.
This gives you the flexibility to handle each module as you see fit.

			Dam.AddModules((bus, dispatcher, lf) => TodoModule.Initialize(bus));

Each module could expose publish, dispatch or store concepts for the controllers to use.

#### Register Services

Due to ASP.NET favour of IoC for controller creation, the following extension is used.

	    public void ConfigureServices(IServiceCollection services)
	    {
		    services.AddFiffi(Dam);
	    }

This registers all Modules and infrastructure. This way you could pick if you want to create module via IoC or initializers.

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


### Dam Creation Console App

				Dam = Dam.CreateDam(() => Fiffi.Streams.MessageVault.Cloud(Configuration), loggerFactory);

#### Run / ListenToSteam

Create a token to gracefully shut down the app on console exit.

			Dam.ListenToStream(token);

### Modules

Each module could handle/store state/views. The module could apply changes to state and publish to the stream using 2PC, or publishing to the steam and mutate state by subscribing to the stream.
The option to lock on instance is provided by the ApplicationService util, and the EventProcessor util.

Dummy example to show lock usage;

#### FooModule

Exctract from Initialize function;

			var l = loggerFactory
				.CreateLogger("FooModule");

			l.LogDebug("Initializing...");

			var locks = new ConcurrentDictionary<Guid, Tuple<Guid, SemaphoreSlim>>();
			var ep = new EventProcessor(locks, l);
			ep.Register<FooEvent>(@event => Task.FromResult(0));

			pub.Register(ep.PublishAsync);

			dispatcher.Register<FooCommand>(command => ApplicationService.Execute(command, () => string.Empty, s => Enumerable.Empty<IEvent>(), pub.PublishAsync, locks));

### Events

Event serilization and conventions - WIP.