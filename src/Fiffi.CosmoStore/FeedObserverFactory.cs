using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;
using System;
using System.Threading.Tasks;

namespace Fiffi.CosmoStore
{
    public class FeedObserverFactory : IChangeFeedObserverFactory
    {
        private readonly Func<IEvent[], Task> dispatcher;
        private readonly Func<string, Type> typeResolver;

        public FeedObserverFactory(Func<IEvent[], Task> dispatcher, Func<string, Type> typeResolver)
        {
            this.dispatcher = dispatcher;
            this.typeResolver = typeResolver;
        }

        public IChangeFeedObserver CreateObserver()
            => new FeedObserver(dispatcher, typeResolver);
    }
}
