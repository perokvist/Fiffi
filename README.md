[![NuGet Version and Downloads count](https://buildstats.info/nuget/Fiffi?includePreReleases=true)](https://www.nuget.org/packages/Fiffi/)

# Fiffi
Exploration kit for eventdriven services.

## Testing

<pre>
┏━━━━━━━━━━━━━━━━━┳━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┳━━━━━━┳━━━━━━━━━━━━━━━━━━┓
┃ Flow            ┃ Waterfall                                                         ┃ Time ┃ Aggregate        ┃
┣━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━╋━━━━━━╋━━━━━━━━━━━━━━━━━━┫
┃ FooCommand      ┃ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░      ┃ 0    ┃ 99a9c841-ebd9-4d ┃
┃ FooEvent        ┃ ░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░      ┃ 1    ┃ 99a9c841-ebd9-4d ┃
┃ FooEvent        ┃ ░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░      ┃ 1    ┃ 4cbcde1d-f12d-42 ┃
┃ BarCommand      ┃ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░      ┃ 2    ┃ 5800daa3-a109-4d ┃
┃ BarEvent        ┃ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓      ┃ 3    ┃ 5800daa3-a109-4d ┃
┃ BarEvent        ┃ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓      ┃ 3    ┃ a5c155cb-3f2d-4a ┃
┃ BarCommand      ┃ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░      ┃ 2    ┃ f20ba848-44f1-45 ┃
┃ BarEvent        ┃ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓      ┃ 3    ┃ f20ba848-44f1-45 ┃
┃ BarEvent        ┃ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓      ┃ 3    ┃ 21e1360f-13af-45 ┃
┗━━━━━━━━━━━━━━━━━┻━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┻━━━━━━┻━━━━━━━━━━━━━━━━━━┛
</pre>

       [Fact]
        public async Task CreateGame()
        {
            //Given
            context.Given(Array.Empty<IEvent>());

            //When
            await context.WhenAsync(new CreateGame { FirstTo = 3, GameId = Guid.NewGuid(), PlayerId = "tester", Title = "Test Game" });

            //Then  
            context.Then((events, visual) => {
                this.helper.WriteLine(visual);
                Assert.True(events.OfType<GameCreated>().Happened());
            });
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
					state => Array.Empty<IEvent>(),
					events => pub(events)
					)
```

The core Fiffi project also includes tools for handling command, query, event dispatching/routing, meta data and concurrecy options.

The vision is to create a kit to get feedback/test from modeling/eventstorming sessions.

