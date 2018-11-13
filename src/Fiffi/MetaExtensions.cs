using System;
using System.Collections.Generic;
using System.Text;

namespace Fiffi
{
	public static class MetaExtensions
	{
		public static void AddTypeInfo(this IDictionary<string, string> meta, IEvent e)
		{
			var t = e.GetType();
			var typeProperties = new Dictionary<string, string>
			{
				{ "name", t.Name },
				{ "eventname", t.Name },
				{ "type.name", t.Name },
				{ "type.assemblyqualifiedname", t.AssemblyQualifiedName },
				{ "type.fullname", t.FullName },
				//{ "type.version", e.GetType().GetVersion() }
			};
			typeProperties.ForEach(x => meta.TryAdd(x.Key, x.Value));
		}

		public static Type GetEventType(this IDictionary<string, string> meta, Func<string, Type> f)
			=> f(meta["type.name"]);

	}
}
