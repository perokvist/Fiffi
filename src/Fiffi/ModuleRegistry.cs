using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fiffi
{
	public class ModuleRegistry : IDisposable
	{
		private readonly IDictionary<Type, object> _modules = new Dictionary<Type, object>();


		private static void DisposeModules(IDictionary<Type, object> modules)
		=> modules
			.Where(m => m.Key.GetInterfaces().Any(i => i == typeof(IDisposable)))
			.Select(x => x.Value as IDisposable)
			.ForEach(d => d.Dispose());

		public void Register(Action<Type, object> f) => _modules.ForEach(m => f(m.Key, m.Value));

		public void AddOrUpdate<T>(T module) => _modules[module.GetType()] = module;
		public void Dispose() => DisposeModules(_modules);
	}
}