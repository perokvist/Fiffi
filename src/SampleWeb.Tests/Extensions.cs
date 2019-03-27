using Fiffi;
using Fiffi.Testing;
using Microsoft.ServiceFabric.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace SampleWeb.Tests
{
	public static class Extensions
	{
		public static Task Enqueue(this Queue<IEvent> q, ITransaction tx, params IEvent[] events)
		{
			events.ForEach(e => q.Enqueue(e));
			return Task.CompletedTask;
		}

		public static StringContent ToContent(this object content)
		{
			var json = JsonConvert.SerializeObject(content);
			return new StringContent(json, Encoding.UTF8, "application/json");
		}

		public static HttpContent CreateHttpContent(this object content)
		{
			HttpContent httpContent = null;

			if (content != null)
			{
				var ms = new MemoryStream();
				SerializeJsonIntoStream(content, ms);
				ms.Seek(0, SeekOrigin.Begin);
				httpContent = new StreamContent(ms);
				httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			}

			return httpContent;
		}

		public static void SerializeJsonIntoStream(object value, Stream stream)
		{
			using (var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
			using (var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None })
			{
				var js = new JsonSerializer();
				js.Serialize(jtw, value);
				jtw.Flush();
			}
		}
	}
}
