using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SampleWeb.Tests
{
	public class DrawTests
	{
		readonly ITestOutputHelper output;

		public DrawTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Foo()
		{
			var id = Guid.NewGuid();
			var foocmd = new FooCommand(id);
			var barcmd = new BarCommand(id);
			var barcmd2 = new BarCommand(id);

			var events = new IEvent[] {
				new FooEvent(Guid.NewGuid())
					.Tap(x => x.Meta.AddMetaData(1, "Test", "Test", foocmd)),
				new FooEvent(Guid.NewGuid())
					.Tap(x => x.Meta.AddMetaData(1, "Test", "Test", foocmd)),
				new BarEvent(Guid.NewGuid())
					.Tap(x => x.Meta.AddMetaData(1, "Test", "Test", barcmd)),
				new BarEvent(Guid.NewGuid())
					.Tap(x => x.Meta.AddMetaData(1, "Test", "Test", barcmd)),
				new BarEvent(Guid.NewGuid())
					.Tap(x => x.Meta.AddMetaData(1, "Test", "Test", barcmd2)),
				new BarEvent(Guid.NewGuid())
					.Tap(x => x.Meta.AddMetaData(1, "Test", "Test", barcmd2)),
			};
			this.output.WriteLine(events.Draw());
			Assert.True(false, "Command not in line");
		}

		public class FooCommand : ICommand
		{

			public FooCommand(Guid id)
			{
				this.AggregateId = new AggregateId(id.ToString());
			}
			public IAggregateId AggregateId { get; }

			public Guid CorrelationId { get; set; } = Guid.NewGuid();
		}

		public class BarCommand : FooCommand
		{
			public BarCommand(Guid id) : base(id) { }
		}


		public class BarEvent : FooEvent
		{
			public BarEvent(Guid id) : base(id) { }
		};

		public class FooEvent : IEvent
		{
			public FooEvent(Guid id)
			{
				this.AggregateId = id;
				this.Meta["eventid"] = Guid.NewGuid().ToString();
			}

			public Guid AggregateId { get; set; }

			public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
		}
	}
}
