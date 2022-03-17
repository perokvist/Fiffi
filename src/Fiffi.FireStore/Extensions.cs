using Fiffi.Serialization;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;

namespace Fiffi.FireStore;
public static class Extensions
{

    public static IServiceCollection AddEventStore(this IServiceCollection services,
       string projectId,
       string storeCollection = "eventstore",
       bool emulatorOnly = false,
       int port = 8080) =>
       services
               .Tap(x =>
               {
                   if (emulatorOnly)
                       Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", $"localhost:{port}");
               })
               .AddSingleton(sp => new FirestoreDbBuilder
               {
                   EmulatorDetection = emulatorOnly ? Google.Api.Gax.EmulatorDetection.EmulatorOnly : Google.Api.Gax.EmulatorDetection.None,
                   ProjectId = projectId,
                   ConverterRegistry = new ConverterRegistry
                   {
                            new EventDataConverter()
                   }
               }
               .Build())
               .Configure<JsonSerializerOptions>(opt => opt.AddConverters())
               .AddSingleton<IEventStore<EventData>, FireStoreEventStore>(sp => new FireStoreEventStore(sp.GetRequiredService<FirestoreDb>()).Tap(x => x.StoreCollection = storeCollection))
               .AddSingleton<IEventStore, EventStore>()
               .AddSingleton<ISnapshotStore>(sp => new SnapshotStore(
                   sp.GetRequiredService<FirestoreDb>(),
                   sp.GetRequiredService<JsonSerializerOptions>()));

    public static JsonSerializerOptions AddConverters(this JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonTimestampConverter());
        options.Converters.Add(new EventRecordConverter());
        options.Converters.Add(new DictionaryStringObjectJsonConverter(
                     (writer, o) =>
                     {
                         return o switch
                         {
                             Timestamp t => writer.Pipe(x =>
                             {
                                 x.WriteStringValue(t.ToDateTime());
                                 return true;
                             }),
                             _ => false
                         };
                     }
                     ));
        return options;
    }

}



