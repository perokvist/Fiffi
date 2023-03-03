using Dapr.Client;
using Fiffi.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fiffi.Dapr;

public class BindingPublisher
{
    readonly DaprClient daprClient;
    readonly JsonSerializerOptions serializerOptions;

    public Func<string, Dictionary<string, string>> MetaProvider { get; set; } =
        (string streamName) => new Dictionary<string, string>();
    public string BindingName { get; set; } = "storage";

    public BindingPublisher(DaprClient daprClient, JsonSerializerOptions serializerOptions)
    {
        this.daprClient = daprClient;
        this.serializerOptions = serializerOptions;
    }

    public Task Publish(params IEvent[] events)
     => Publish(daprClient, this.BindingName, serializerOptions, this.MetaProvider, events);

    public static Task Publish(
        DaprClient client,
        string bindingName,
        JsonSerializerOptions serializerOptions,
        Func<string, Dictionary<string, string>> metaProvider,
        params IEvent[] events)
     => Task.WhenAll(events
            .Select(x => ($"{x.Event.GetType().Name}-{x.Meta.GetMetaOrDefault(nameof(EventMetaData.EventId), Guid.NewGuid())}", x.ToMap(serializerOptions)))
            .Select(x => client.InvokeBindingAsync(bindingName, "create", x.Item2, metaProvider(x.Item1)))
         );

    public static Func<string, Dictionary<string, string>> AzureProvider()
        => blobName => new Dictionary<string, string>()
                {
                    { "blobName", blobName },
                    { "ContentType", "application/json" }
                };
}
