using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using MessageVault.Api;
using MessageVault.Cloud;
using MessageVault.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fiffi
{
	public class Dam
	{
		private readonly IConfiguration _configuration;
		private readonly ILoggerFactory _loggerFactory;
		private readonly IDictionary<Type, IDisposable> _modules;

		public IEventBus Pub { get; }
		public CommandDispatcher Dispatcher { get; }

		private Dam(IEventBus pub, CommandDispatcher dispatcher, ILoggerFactory loggerFactory)
		{
			_loggerFactory = loggerFactory;
			Pub = pub;
			Dispatcher = dispatcher;
			_modules = new Dictionary<Type, IDisposable>();
		}

		public static Dam CreateMemoryDam(IConfiguration configuration, ILoggerFactory loggerFactory)
			=>  new Dam(
				new MessageVaultEventBus(new MemoryClient(), new MemoryCheckpointReaderWriter(), configuration["fiffi::stream-name"] ?? "test-stream"),	//TODO fix null
				new CommandDispatcher(),
				loggerFactory
				);

		public static Dam CreateCloudDam(IConfiguration configuration, ILoggerFactory loggerFactory)
			=> new Dam(
				new MessageVaultEventBus(new Client(null, null, null), new CloudCheckpointWriter(null), configuration["fiffi::stream-name"]),
				new CommandDispatcher(),
				loggerFactory
				);

		internal IServiceCollection Register(IServiceCollection serviceCollection) =>
			serviceCollection
			.Tap(x => serviceCollection.AddInstance(this))	//TODO remove and add property to startup
			.Tap(x => serviceCollection.AddInstance(Pub))
			.Tap(x => serviceCollection.AddInstance(Dispatcher))
			.Tap(x => _modules.ForEach(m => serviceCollection.AddSingleton(m.Key, sp => m.Value)));


		public void AddModules(params Func<IEventBus, IDisposable>[] modules) =>
			modules.ForEach(m => AddModules((bus, dispatcher, lf) => m(bus)));

		public void AddModules(params Func<IEventBus, CommandDispatcher, ILoggerFactory, IDisposable>[] modules) =>
			modules
			.ForEach(m => m(Pub, Dispatcher, _loggerFactory)
			.Tap(x => _modules[x.GetType()] = x));	 //Disposable not used, and duplication trouble - hook to run ?

		public void ListenToStream(CancellationToken token) =>
			Pub.Run(token, _loggerFactory.CreateLogger<IEventBus>());
	}
}