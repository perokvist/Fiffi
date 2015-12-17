using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Fiffi.Testing
{
	public static class Assertion
	{

		public static async Task RunCaseAsync(Env.UseCaseData @case, Dam dam, HttpClient client)
		{
			var assertEvents = @case.ThenEvents != null && @case.ThenEvents.Any();
			var apiInteraction = @case.When != null;
			Nest nest = null;

			dam.AddModules(bus =>
			{
				nest = Nest.InitializeAsync(bus, @case.Given.ToArray()).Result;
				return Task.FromResult(0);
			});

			if (apiInteraction)
			{
				var r = await client.SendAsync(@case.When);
				string expectedString;

				if (@case.ThenResponse is string)
					expectedString = @case.ThenResponse.ToString();
				else
					expectedString = JsonConvert.SerializeObject(@case.ThenResponse);

				Assert.Equal(expectedString, await r.Content.ReadAsStringAsync());
			}

			if (assertEvents)
			{
				// MessageVault.MessageReader sleeps for 1sec :I
				if (!nest.Happend.Any()) //TODO skip if fixture is use for multiple tests
					await Task.Delay(1000);

				Assert.Collection(nest.Happend,
					@case.ThenEvents.Select(e => new Action<IEvent>(@event => Assert.Equal(ToIgnoreString(e), ToIgnoreString(@event)))).ToArray());
			}

		}

		private static string ToIgnoreString(IEvent e1)
		{
			var s1 = JsonConvert.SerializeObject(e1);
			var json1 = JObject.Parse(s1);
			json1.Property(nameof(IEvent.CorrelationId)).Remove();
			json1.Property(nameof(IEvent.EventId)).Remove();
			return json1.ToString();
		}
	}
}