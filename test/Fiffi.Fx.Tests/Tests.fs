module Tests

open System
open Xunit
open Fiffi.Fx.App
open Fiffi.Fx.Serialization
open Fiffi.Fx.CloudEvents
open System.Text.Json
open System.Text.Json.Serialization
open RPS.Game
open RPS.App
open Fiffi.CloudEvents

//[<Fact>]
//let ``Serialize CloudEvent with union`` () =
//    let options = JsonSerializerOptions()
//    options.Converters.Add(JsonFSharpConverter())

//    let events =
//        GameCreated
//            { GameId = Guid.NewGuid()
//              Title = "test" }
//        |> createCloudEvent
//        |> toJson
//        |> toEvent
//        |> dataAs<Events> options

//    match events with
//    | GameCreated e -> Assert.Equal("test", e.Title)
//    | _ -> raise (new Exception("boom"))

[<Fact>]
let ``Dipatch event spy`` () = async {
    let mutable events = List.empty<CloudNative.CloudEvents.CloudEvent>
    let pub (e:CloudNative.CloudEvents.CloudEvent list) =
        events <- List.append events e
        async.Return()
    
    let store = Fiffi.CloudEvents.CloudEventStore.CreateInMemoryStore()
    let app = RPS.App.init store pub

    let cmd:Command = CreateGame { GameId = Guid.NewGuid(); PlayerId = "test"; Title = "test game"; Rounds = 1; CorrelationId = Guid.NewGuid(); CausationId = Guid.NewGuid() } 
    let! _ = app cmd
    let firstEvent = events.Item 0 
    let secondEvent = events.Item 1

    Assert.False(List.isEmpty events)
    Assert.True(firstEvent.Extension<EventStoreMetaDataExtension>().MetaData.EventVersion = (1 |> int64))
    Assert.Equal("creategame", firstEvent.Extension<EventMetaDataExtension>().MetaData.TriggeredBy)
    Assert.NotEqual(firstEvent.Extension<EventMetaDataExtension>().MetaData.EventId, secondEvent.Extension<EventMetaDataExtension>().MetaData.EventId)

    }
