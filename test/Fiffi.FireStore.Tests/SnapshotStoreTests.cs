using Fiffi.Serialization;
using Fiffi.Testing;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using static Fiffi.FireStore.DocumentPathProviders;
using PathProvider = System.Func<Google.Cloud.Firestore.FirestoreDb, Fiffi.FireStore.DocumentPathProviders.StreamContext, System.Threading.Tasks.Task<Fiffi.FireStore.DocumentPathProviders.StreamPaths>>;


namespace Fiffi.FireStore.Tests;

public class SnapshotStoreTests
{
    private FirestoreDb store;
    private readonly JsonSerializerOptions options;

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
            ProjectId = "demo-project"
        };
        store = b.Build();
    }

    public static PathProvider Test() =>
        async (store, ctx) =>
        {
            var modCtx = ctx with { Key = $"Clients/TestClient/eventstore/{ctx.Key}" };
            return await SubCollectionAll()(store, modCtx);
        };

    public static IEnumerable<object[]> GetProviders()
        => new List<object[]>
        {
            new object[] { All() },
            new object[] { SubCollectionByPartition() },
            new object[] { Test() }
        };

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task ApplySnapshotAsync(PathProvider p)
    {
        var snapshotStore = new SnapshotStore(store, options) { DocumentPathProvider = p };

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
        var snapshotStore = new SnapshotStore(store, options) { DocumentPathProvider = SubCollectionByPartition() };

        var fullPath = new Uri($"/Clients/EvilCorp/messages/{Guid.NewGuid()}", UriKind.Relative);
        var key = fullPath.ToString();
        await snapshotStore.Apply<TestState>
            (key, current => current with { Version = 99, Created = DateTime.UtcNow });

        var snap = await snapshotStore.Get<TestState>(key);

        Assert.Equal(99, snap.Version);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApplyWithSubCollectionSnapshotConvensionKey()
    {
        var snapshotStore = new SnapshotStore(store, options) { DocumentPathProvider = SubCollectionByPartition() };

        var fullPath = new Uri($"/Clients/EvilCorp/messages/{Guid.NewGuid()}|snapshot", UriKind.Relative);
        var key = fullPath.ToString();

        await snapshotStore.Apply<TestState>
            (key, current => current with { Version = 99, Created = DateTime.UtcNow });

        var snap = await snapshotStore.Get<TestState>(key);

        Assert.Equal(99, snap.Version);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task ApplyWithSubCollectionSnapshotConvensionKeyNonePath(PathProvider p)
    {
        var snapshotStore = new SnapshotStore(store, options) { DocumentPathProvider = p };

        var key = $"testname-{Guid.NewGuid()}";

        await snapshotStore.Apply<TestState>
            (key, current => current with { Version = 99, Created = DateTime.UtcNow });

        var snap = await snapshotStore.Get<TestState>(key);

        Assert.Equal(99, snap.Version);
    }
}
