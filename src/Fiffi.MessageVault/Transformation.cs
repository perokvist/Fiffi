using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MessageVault;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Fiffi.MessageVault
{
	public static class Transformation
	{
		public class Key
		{
			private Key(string eventName, Guid eventId)
			{
				EventName = eventName;
				EventId = eventId;
			}

			public Guid EventId { get; }
			public string EventName { get; }

			public static Key FromString(string key)
				=> new Key(key.Split('|').First(), Guid.Parse(key.Split('|').Last()));

			public static string CreateKey(Type type, Guid eventId)
				=> $"{type.Name}|{GetVersion(type)}|{type.FullName}|{eventId}";
		}

		public static Message ToMessage(IEvent @event)
		{
			var json = JsonConvert.SerializeObject(@event, Formatting.None, new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			});

			return Message.Create(Key.CreateKey(@event.GetType(), @event.EventId), Encoding.Default.GetBytes(json));
		}

		public static IEvent ToEvent(MessageWithId message, Type type)
			=> JsonConvert.DeserializeObject(Encoding.Default.GetString(message.Value), type) as IEvent;

		private static string GetVersion(Type type)
			=> type != null ? Assembly.GetAssembly(type).GetName().Version.ToString() : string.Empty;

	}
}
