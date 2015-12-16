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
		private class Key
		{
			public Type Type { get; }
			public string Version => Type != null ? Assembly.GetAssembly(Type).GetName().Version.ToString() : string.Empty;

			private static readonly IDictionary<string, Type> typeCache = new ConcurrentDictionary<string, Type>();

			public Key(string key)
			{
				var name = key.Split('|').First();

				if (typeCache.ContainsKey(name))
					Type = typeCache[name];
				else
				{
					Type = FindEventType(name);
					if (!typeCache.ContainsKey(name))
						typeCache.Add(name, Type);
				}
			}

			public Key(Type type)
			{
				Type = type;
			}

			public override string ToString() => $"{Type.Name}|{Version}|{Type.FullName}";

			private static Type FindEventType(string eventName) 
				=> AppDomain
						.CurrentDomain.GetAssemblies()
						.SelectMany(a => a.GetTypes())
						.FirstOrDefault(t => t.FullName.EndsWith(eventName));
		}

		public static Message ToMessage(IEvent @event)
		{
			var json = JsonConvert.SerializeObject(new { @event.Meta, @event.Values }, Formatting.None, new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			});

			return Message.Create(new Key(@event.GetType()).ToString(), Encoding.Default.GetBytes(json));
		}

		public static IEvent ToEvent(MessageWithId message)
		{
			var key = new Key(Encoding.Default.GetString(message.Key));
			if (key.Type == null)
				return null;

			return JsonConvert.DeserializeObject(Encoding.Default.GetString(message.Value), key.Type) as IEvent;
		}
	}
}
