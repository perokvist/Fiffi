using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Fiffi.Testing
{
	public static class Env
	{
		public static Context CreateContext(Action<IApplicationBuilder> a, Action<IServiceCollection> a2)
			=>	new Context(a, a2);

		public class Context : IDisposable
		{
			private readonly ServerFixture fixture;

			internal Context(Action<IApplicationBuilder> a, Action<IServiceCollection> a2)
			{
				fixture = ServerFixture.Create(a, a2);
			}

			public Task RunAsync(Func<HttpClient, Dam, Task> @case)
				=> fixture.RunAsync(@case);

			public Task RunAsync(UseCaseData @case) 
				=> fixture.RunAsync((client, dam) => Assertion.RunCaseAsync(@case, dam, client));

			public void Dispose() => fixture.Dispose();
		}

		public static async Task RunAsync(
			Action<IApplicationBuilder> a,
			Action<IServiceCollection> a2,
			params UseCaseData[] @cases)
		{
			await ServerFixture.UseAsync(a, a2, async (client, dam) =>
			{
				foreach (var @case in @cases)
				{
					await Assertion.RunCaseAsync(@case, dam , client);
				}
			});
		}



		public static UseCaseData UseCase(string name, IEnumerable<IEvent> given = null, HttpRequestMessage when = null, object thenResponse = null,
			IEnumerable<IEvent> thenEvents = null)
				=>  new UseCaseData(name, given ?? Spec.GivenEvents(null), when, thenResponse, thenEvents);
		

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