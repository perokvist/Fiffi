using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiffi.Testing
{
	public static class Env
	{
		public static async Task RunAsync(
			Action<IApplicationBuilder> a, 
			Action<IServiceCollection> a2,
			UseCaseData @case)
		{
			await ServerFixture.UseAsync(a, a2, async (client, pub) =>
			{
				var happend = new List<IEvent>();
				var assertEvents = @case.ThenEvents != null && @case.ThenEvents.Any();

				if (assertEvents)
					pub.Register(events => 
					 {
						 happend.AddRange(events);
						 return Task.FromResult(0);
					 });

				await pub.PublishAsync(@case.Given.ToArray());

				if (@case.When != null)
				{
					var r = await client.SendAsync(@case.When);
					Assert.Equal(@case.ThenResponse, await r.Content.ReadAsStringAsync());
				}

				if (assertEvents)
				{
					// MessageVault.MessageReader sleeps for 1sec :I
					if (!happend.Any())
						await Task.Delay(1000);

					Assert.Collection(@case.ThenEvents, @event => Assert.True(happend.Any(e => e.GetType() == @event.GetType())));
				}
			});
		}

		public static UseCaseData UseCase(string name, IEnumerable<IEvent> given, HttpRequestMessage when = null, object thenResponse = null,
			IEnumerable<IEvent> thenEvents = null)
		{
			return new UseCaseData(name, given, when, thenResponse, thenEvents);
		}

		public class UseCaseData
		{
			public string Name;
			public IEnumerable<IEvent> Given;
			public HttpRequestMessage When;
			public object ThenResponse;
			public IEnumerable<IEvent> ThenEvents;

			internal UseCaseData(string name, IEnumerable<IEvent> given, HttpRequestMessage when, object thenResponse, IEnumerable<IEvent> thenEvents)
			{
				Name = name;
				Given = given;
				When = when;
				ThenResponse = thenResponse;
				ThenEvents = thenEvents;
			}
		}
	}
}