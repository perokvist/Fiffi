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
			table.Columns.Add(new AsciiColumn("Flow", 15));
			table.Columns.Add(new AsciiColumn("Waterfall", 65));
			table.Columns.Add(new AsciiColumn("Time", 4));
			table.Columns.Add(new AsciiColumn("Aggregate", 16));

			var blocks = events.BuildBlocks();

			var total = events.GroupBy(x => x.OccuredAt().ToString()).Count() * 2;

			blocks.ForEach((x, i) =>
				table.Rows.Add(new List<string> { x.Name, DrawBar(0, total, x.Time, x.Time + 1, '\u2593', '\u2591', 60), x.Time.ToString(), x.AggregateId })
			);

			return table.ToString();
		}

		static IEnumerable<(string Name, int Time, string AggregateId)> BuildBlocks(this IEvent[] events)
		{
			var g = events.GroupBy(x => $"{x.GetTrigger()} : {x.GetTriggerId()}");
			var blocks = new List<(string, int, string)>();

			var commandPosition = 0;
			var lastOccured = string.Empty;

			g.ForEach((x, i) =>
			{
				var occured = x.First().OccuredAt().ToString();
				commandPosition = lastOccured == occured ? commandPosition : i * 2;
				lastOccured = occured;
				blocks.Add((x.Key.Split(':')[0].Trim(), commandPosition, x.First().AggregateId.ToString()));
				blocks.AddRange(x.Select(e => (e.GetType().Name, commandPosition + 1, e.AggregateId.ToString())));
			});

			return blocks;
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
