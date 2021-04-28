module Tests

open System
open System.Text.Json
open System.Text.Json.Serialization
open Xunit
open Fiffi.Fx.Serialization
open Fiffi.Fx.CloudEvents
open Fiffi.Fx.Interop
open Fiffi.Fx.Snapshot
open Fiffi.CloudEvents
open RPS.Game
open RPS.Read


[<Fact>]
let ``maybe to option`` () =
    let m = Fiffi.Maybe<GameView>.None
    let o = toOption m

    match o with
    | Option.None -> Assert.True(true)
    | _ -> Assert.True(false, "wrong option")

[<Fact>]
let ``snapshot get`` () = async {
        let snapStore = new Fiffi.InMemory.InMemorySnapshotStore()
   
        let! o = get<GameView> snapStore "test"
    
        match o with
        | Option.None -> Assert.True(true)
        | _ -> Assert.True(false, "wrong option")
    }
    
[<Fact>]
let ``Serialize CloudEvent with union`` () =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    let events =
        GameCreated
            { GameId = Guid.NewGuid()
              Title = "test" }
        |> createCloudEvent
        |> toJson
        |> toEvent
        |> dataAs<Events> options

    match events with
    | GameCreated e -> Assert.Equal("test", e.Title)
    | _ -> raise (new Exception("boom"))

[<Fact>]
let ``Dipatch event spy`` () = async {
    let mutable events = List.empty<CloudNative.CloudEvents.CloudEvent>
    let pub (e:CloudNative.CloudEvents.CloudEvent seq) =
        events <- List.append events (e |> List.ofSeq)
        async.Return()
    
    let store = Fiffi.CloudEvents.CloudEventStore.CreateInMemoryStore()
    let app = RPS.App.init store pub

    let cmd:Command = CreateGame { GameId = Guid.NewGuid(); PlayerId = "test"; Title = "test game"; Rounds = 1; CorrelationId = Guid.NewGuid(); CausationId = Guid.NewGuid() } 
    let! _ = app.Dispatch cmd
    let firstEvent = events.Item 0 
    let secondEvent = events.Item 1

    Assert.False(List.isEmpty events)
    Assert.True(firstEvent.Extension<EventStoreMetaDataExtension>().MetaData.EventVersion = (1 |> int64))
    Assert.Equal("creategame", firstEvent.Extension<EventMetaDataExtension>().MetaData.TriggeredBy)
    Assert.NotEqual(firstEvent.Extension<EventMetaDataExtension>().MetaData.EventId, secondEvent.Extension<EventMetaDataExtension>().MetaData.EventId)

    }

[<Fact>]
let ``query`` () = async {
    let mutable events = List.empty<CloudNative.CloudEvents.CloudEvent>
    let pub (e:CloudNative.CloudEvents.CloudEvent seq) =
        events <- List.append events (e |> List.ofSeq)
        async.Return()
    
    let store = Fiffi.CloudEvents.CloudEventStore.CreateInMemoryStore()
    let app = RPS.App.init store pub
    let q = Query.GameQuery { Id = Guid.NewGuid()} 

    let! r = app.Query q 

    match r with
        | GameView v -> Assert.IsType<GameView>(v) 
        | _ -> raise (new Exception("wrong view"))
    |> ignore

    }

[<Fact>]
let ``query game view title`` () = async {

    let pub (e:CloudNative.CloudEvents.CloudEvent seq) =
        async.Return()
    
    let store = Fiffi.CloudEvents.CloudEventStore.CreateInMemoryStore()
    let app = RPS.App.init store pub
    let gameId = Guid.NewGuid()

    let cmd:Command = CreateGame { GameId = gameId; PlayerId = "test"; Title = "test game"; Rounds = 1; CorrelationId = Guid.NewGuid(); CausationId = Guid.NewGuid() } 
    let! _ = app.Dispatch cmd
    
    let q = Query.GameQuery { Id = gameId} 

    let! r = app.Query q 

    match r with
        | GameView v -> Assert.Equal("test game", v.Title) 
        | _ -> raise (new Exception("wrong view"))
        |> ignore
    }

