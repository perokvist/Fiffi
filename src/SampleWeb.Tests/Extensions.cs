using Fiffi;
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

		public static bool Happened(this IEnumerable<IEvent> events) => events.Count() >= 1;

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

		public static void Then(this TestContext context, Action<IEvent[], string> f)
		=> context.Then(events =>
		{
			var t = events.Draw();
			f(events, t);
		});

		public static string Draw(this IEvent[] events)
		{
			var table = new AsciiTable();
			table.Columns.Add(new AsciiColumn("Flow", 20));
			table.Columns.Add(new AsciiColumn("Waterfall", 70));
			table.Columns.Add(new AsciiColumn("Time", 10));

			var g = events.GroupBy(x => x.GetTrigger());
			var blocks = new List<(string, int)>();

			g.ForEach((x, i) => blocks.Tap(b => b.Add((x.Key, i * 2))).AddRange(x.Select(e => (e.GetType().Name, ((i * 2) + 1)))));

			var total = blocks.Count + 1;

			blocks.ForEach((x, i) =>
				table.Rows.Add(new List<string> { x.Item1, DrawBar(0, total, x.Item2, x.Item2 + 1, '\u2593', '\u2591', 80), x.Item2.ToString() })
			);

			return table.ToString();
		}

		static string DrawBar(
			int totalStart, int totalEnd,
			int barStart, int barEnd,
			char barFilling, char backgroundFilling,
			int length)
		{
			var total = totalEnd - totalStart;
			var startIndex = (int)Math.Round((barStart - totalStart) / (decimal)total * length, 0);
			var endIndex = (int)Math.Round((barEnd - totalStart) / (decimal)total * length, 0);

			var before = new string(backgroundFilling, Math.Max(startIndex, 0));
			var bar = new string(barFilling, Math.Max(endIndex - startIndex, 0));
			var returnValue = (before + bar).PadRight(length, backgroundFilling);
			return returnValue;
		}

		static string DrawTimeBar(
			DateTime totalStart, DateTime totalEnd,
			DateTime barStart, DateTime barEnd,
			char barFilling, char backgroundFilling,
			int length)
		{
			var total = totalEnd.Ticks - totalStart.Ticks;
			var startIndex = (int)Math.Round((barStart.Ticks - totalStart.Ticks) / (decimal)total * length, 0);
			var endIndex = (int)Math.Round((barEnd.Ticks - totalStart.Ticks) / (decimal)total * length, 0);

			var before = new string(backgroundFilling, Math.Max(startIndex, 0));
			var bar = new string(barFilling, Math.Max(endIndex - startIndex, 0));
			return (before + bar).PadRight(length, backgroundFilling);
		}
	}
}
