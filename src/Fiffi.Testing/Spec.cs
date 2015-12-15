using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Fiffi.Testing
{
	public static class Spec
	{
		public static IEnumerable<IEvent> GivenEvents(params IEvent[] es) => es ?? Enumerable.Empty<IEvent>();

		public static HttpRequestMessage PostJSON(string url, object values)
		{
			return new HttpRequestMessage(HttpMethod.Post, new Uri(url))
			{
				Content = new StringContent(JsonConvert.SerializeObject(values), Encoding.UTF8, "application/json")
			};
		}

		public static HttpRequestMessage GetJSON(string url, object values)
		{
			return new HttpRequestMessage(HttpMethod.Get, new Uri(url))
			{
				Content = new StringContent(JsonConvert.SerializeObject(values), Encoding.UTF8, "application/json")
			};
		}


		public static IEnumerable<IEvent> Events(params IEvent[] es) => es ?? Enumerable.Empty<IEvent>();
	}
}