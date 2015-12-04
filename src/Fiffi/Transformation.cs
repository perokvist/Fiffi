using System;
using System.Linq;
using System.Text;
using MessageVault;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Fiffi
{
    public static class Transformation
    {
	    public static Message ToMessage(IEvent @event)
	    {
		    var json = JsonConvert.SerializeObject(new {@event.Meta, @event.Values}, Formatting.None, new JsonSerializerSettings
		    {
			    ContractResolver = new CamelCasePropertyNamesContractResolver()
		    });
			var jObject = JObject.Parse(json);

		    return Message.Create(@event.GetType().FullName, Encoding.Default.GetBytes((string) jObject.ToString(Formatting.None)));
		}

		public static IEvent ToEvent(MessageWithId message)
		{
			var typeName = Encoding.Default.GetString(message.Key);

			var type = Type.GetType(typeName) ??
			           AppDomain.CurrentDomain.GetAssemblies()
				           .Select(a => a.GetType(typeName))
				           .FirstOrDefault(t => t != null);

			var o = JsonConvert.DeserializeObject(Encoding.Default.GetString(message.Value), type);

			return o as IEvent;
		}
    }
}
