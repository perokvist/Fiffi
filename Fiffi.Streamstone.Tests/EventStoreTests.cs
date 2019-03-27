using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Streamstone;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Streamstone.Tests
{
	public class EventStoreTests
	{
		private CloudTable table;

		public EventStoreTests()
		{
			var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true;");

			table = storageAccount.CreateCloudTableClient().GetTableReference("fiffiintegration");
			table.DeleteIfExistsAsync().Wait();
			table.CreateIfNotExistsAsync().Wait();
		}

		[Fact]
		[Trait("Category", "Integration")]
		public async Task WriteReadAsync()
		{
			var es = new StreamStoneEventStore(table, TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEvent))));

			var id = Guid.NewGuid();
			var e = new TestEvent()
			{
				AggregateId = id,
				Meta = new Dictionary<string, string>() {
				{ "EventId", Guid.NewGuid().ToString() }
			}
			};

			await es.AppendToStreamAsync("testing", 0, new[] { e });
			var r = await es.LoadEventStreamAsync("testing", 1);
			Assert.Equal(1, r.Version);
		}

		[Fact]
		[Trait("Category", "Integration")]
		public async Task DublicateEventThrowsAsync()
		{
			var es = new StreamStoneEventStore(table, TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEvent))));
			var eventid = Guid.NewGuid().ToString();

			var id = Guid.NewGuid();
			var e = new TestEvent()
			{
				AggregateId = id,
				Meta = new Dictionary<string, string>() {
				{ "EventId", eventid }
			}
			};


			await es.AppendToStreamAsync("testing", 0, new[] { e });

			var e2 = new TestEvent()
			{
				AggregateId = id,
				Meta = new Dictionary<string, string>() {
				{ "EventId", eventid }
			}
			};

			await Assert.ThrowsAsync<DuplicateEventException>(() => es.AppendToStreamAsync("testing", 1, new[] { e2 }));
		}

		public class TestEvent : IEvent
		{
			public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();

			public Guid AggregateId { get; set; }
		}
	}
}
