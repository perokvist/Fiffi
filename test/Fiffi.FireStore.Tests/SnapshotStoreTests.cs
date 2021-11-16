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
        await snapshotStore.Apply<TestState>($"test-{Guid.NewGuid()}", current => current with { Version = 99 });
    }

}
