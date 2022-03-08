using Fiffi.Serialization;
using Fiffi.Testing;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.FireStore.Tests;

public class SnapshotStoreTests
{
    private FirestoreDb store;
    private readonly JsonSerializerOptions options;
    private SnapshotStore snapshotStore;

    public SnapshotStoreTests()
    {
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080");

        options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            .Tap(x => x.Converters.Add(new DictionaryStringObjectJsonConverter()))
            .Tap(x => x.Converters.Add(new EventRecordConverter()))
            .Tap(x => x.Converters.Add(new JsonTimestampConverter()))
            .Tap(x => x.PropertyNameCaseInsensitive = true);

        var b = new FirestoreDbBuilder
        {
            EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly,
            ProjectId = "dummy-project"
        };
        store = b.Build();
        snapshotStore = new SnapshotStore(store, options);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApplySnapshotAsync()
    {
        var key = $"test-{Guid.NewGuid()}";
        await snapshotStore.Apply<TestState>
            (key, current => current with { Version = 99, Created = DateTime.UtcNow });

        var snap = await snapshotStore.Get<TestState>(key);

        Assert.Equal(99, snap.Version);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApplyWithSubCollectionKey()
    {
        snapshotStore.DocumentPathProvider = 
            DocumentPathProviders.SubCollection();

        var fullPath = new Uri($"/clients/EvilCorp/messages/{Guid.NewGuid()}", UriKind.Relative);
        var key = fullPath.ToString();
        await snapshotStore.Apply<TestState>
            (key, current => current with { Version = 99, Created = DateTime.UtcNow });

        var snap = await snapshotStore.Get<TestState>(key);

        Assert.Equal(99, snap.Version);
    }
}
