using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Fiffi.CosmoStore
{
    public class FeedObserverFactory : IChangeFeedObserverFactory
    {
        private readonly Func<IEvent[], Task> dispatcher;
        private readonly Func<string, Type> typeResolver;
        private readonly ILogger logger;

        public FeedObserverFactory(Func<IEvent[], Task> dispatcher, Func<string, Type> typeResolver, ILogger logger)
        {
            this.dispatcher = dispatcher;
            this.typeResolver = typeResolver;
            this.logger = logger;
        }

        public IChangeFeedObserver CreateObserver()
            => new FeedObserver(dispatcher, typeResolver, logger);
    }
}
