using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using MessageVault.Api;
using MessageVault.Cloud;
using MessageVault.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Fiffi
{
	public class Dam
	{
		private readonly ILoggerFactory _loggerFactory;
		private readonly ModuleRegistry _modules;

		private IEventBus Pub { get; }

		private CommandDispatcher Dispatcher { get; }

		private Dam(IEventBus pub, CommandDispatcher dispatcher, ILoggerFactory loggerFactory)
		{
			_loggerFactory = loggerFactory;
			_modules = new ModuleRegistry();
			Pub = pub;
			Dispatcher = dispatcher;
		}

		public static Dam CreateDam(Func<IEventBus> f, ILoggerFactory loggerFactory)
			=> new Dam(f(), new CommandDispatcher(), loggerFactory);

		internal IServiceCollection Register(IServiceCollection services) =>
		services
		.Tap(x => services.AddInstance(this))   //TODO remove and add property to startup
		.Tap(x => services.AddInstance(Pub))
		.Tap(x => services.AddInstance(Dispatcher))
		.Tap(x => _modules.Register((type, m) => services.AddSingleton(type, sp => m)));


		public void AddModules(params Func<IEventBus, object>[] modules) =>
			modules.ForEach(m => AddModules((bus, dispatcher, lf) => m(bus)));

		public void AddModules(params Func<IEventBus, CommandDispatcher, ILoggerFactory, object>[] modules) =>
			modules
			.ForEach(m => m(Pub, Dispatcher, _loggerFactory)
			.Tap(x => _modules.AddOrUpdate(x)));

		public void ListenToStream(CancellationToken token) =>
			token
			.Tap(x => x.Register(() => _modules.Dispose()))
			.Tap(x => Pub.Run(x, _loggerFactory.CreateLogger<IEventBus>()));
	}
}