<img src="fiffi.png" width="200" alt="Fiffi logo" />

# Fiffi
Exploration kit for eventdriven services.

		[Fact]
		public async Task AddItemToCartAsync()
		{
			var context = new TestContext();

			//Given
			context.Given(new IEvent[0]);

			//When
			await context.When(new AddItemCommand(Guid.NewGuid()));

			//Then
			context.Then(events => Assert.True(events.OfType<ItemAddedEvent>().Count() == 1));
		}

At it's core is an defenition of an IEventStore and the IEvent.

```
	public interface IEventStore
	{
		Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events);

		Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, int version);
	}
```

The kit then focuses on creating a module of usecases with eventdriven state and flows.

The ApplicationService.ExecuteAsync is targeted on the infrastructure part of an application service.

```
ApplicationService.ExecuteAsync(
					store,
					command,
					state => new IEvent[0],
					events => pub(events)
					)
```

The core Fiffi project also includes tools for handling command, query, event dispatching/routing, meta data and concurrecy options.

The vision is to create a kit to get feedback/test from modeling/eventstorming sessions.

### Implementations

The project currenlty only includes a implementation of IEventStore for Service Fabric's reliable collections. Each implementation might add utils that suites that infrastructure.