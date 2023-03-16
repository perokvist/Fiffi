using Dapr.Client;
using Fiffi.Serialization;
using Fiffi.Testing;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Dapr.Tests;
public class BindingPublisherTests
{
    private JsonSerializerOptions options;
    private DaprClient client;

    public BindingPublisherTests()
    {
        Environment.SetEnvironmentVariable("DAPR_GRPC_PORT", "50006");

        var inDapr = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") != null;
        options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            .Tap(x => x.Converters.Add(new EventRecordConverter()))
            .Tap(x => x.PropertyNameCaseInsensitive = true);
        client = new DaprClientBuilder()
                .UseJsonSerializationOptions(options)
                .Build();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PublishAsync()
    {
        var @event = EventEnvelope.Create("test", new TestEventRecord("hellos"));
        await BindingPublisher.Publish(client, "storage", options, BindingPublisher.AzureProvider(), @event);
    }
}
