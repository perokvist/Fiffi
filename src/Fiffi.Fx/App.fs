namespace Fiffi.Fx
open Fiffi.CloudEvents
open CloudNative.CloudEvents
open System
open System.Text.Json
open System.Text.Json.Serialization


module Serialization =

    let toJson cloudEvent = Extensions.ToJson cloudEvent

    let toEvent (json:string) = Extensions.ToEvent json

module CloudEvents =
    let createCloudEvent data : CloudEvent = 
        let e = CloudEvent("test", Uri("urn:test"), null, DateTime.UtcNow)
        e.Data <- data
        e

    let dataAs<'T> options cloudEvent = Extensions.DataAs<'T>(cloudEvent, options)

module App =
   
    type applicationService = EventStore.StreamName -> (CloudEvent list -> Async<CloudEvent list>) -> Async<unit>

    let mapAsync f a = async { 
        let! v = a
        return (f v)
    }

    let pub events = 
        async {
            return 0
        } 
        |> Async.Ignore
    let store = Fiffi.CloudEvents.CloudEventStore null
    let streamExecution = EventStore.execute store pub
    let functionThatCallsDomainModel = streamExecution("myStream" |> EventStore.StreamName)

    let dummyHandle state = [ GameStarted { GameId = Guid.NewGuid(); PlayerId = "test" }; GameCreated { GameId = Guid.Empty; Title = "testing" } ]

    let createGame (history : CloudEvent list) : Async<CloudEvent list> =
        async {
            let options = JsonSerializerOptions()
            options.Converters.Add(JsonFSharpConverter())
            let d = { GameId = Guid.Empty }
            let state = List.fold apply d (history |> List.map(fun x -> x |> CloudEvents.dataAs<Events> null))
            return dummyHandle state |> List.map(fun x -> x |> CloudEvents.createCloudEvent)
        }

    let r = functionThatCallsDomainModel createGame

    //let createGame : applicationService = fun streamName f -> 
    //    async {
    //        let current = f
    //    } 


    //type Append = EventStore.StreamName -> EventStore.Version -> CloudEvent list -> Async<EventStore.Version>
    //type AsyncAppend = EventStore.StreamName -> Async<EventStore.Version * CloudEvent list> -> Async<EventStore.Version>
    //type Read = EventStore.StreamName -> EventStore.Version -> Async<EventStore.Version * CloudEvent list>
    //type Handler<'TCommand, 'TState> = 'TCommand -> EventStore.Version * 'TState -> EventStore.Version * CloudEvent list
    //type AsyncHandler<'TCommand, 'TState> = 'TCommand -> Async<EventStore.Version * 'TState> -> Async<EventStore.Version * CloudEvent list>
    //type AsyncRehydrate<'T> = Async<EventStore.Version * IEvent list> -> Async<EventStore.Version * 'T>

    //let asyncRehydrate<'TState> (defaultState:'TState) f : AsyncRehydrate<'TState> = 
    //    fun x -> x |> mapAsync(fun(version, events) -> (version ,(State.RehydrateStateFrom events defaultState f)))
    
    //let asyncHandler (h:Handler<ICommand, GameState>) : AsyncHandler<ICommand, GameState> = 
    //    fun cmd state -> state |> mapAsync(fun(state) -> (h cmd state))
    
    //let asyncAppend (a:Append) : AsyncAppend = fun streamName t -> 
    //    let appender = a(streamName)
    //    async {
    //     let! (version, events) = t
    //     return! appender version events
    //    }

    //let applicationService<'TCommand, 'TState> (store) (defaultState:'TState) (apply) (cmd:'TCommand) (cmdMeta:ICommand) (f) : Async<unit> =
    //    let startFrom = (int64 0) |> EventStore.Version

    //    let append : Append = fun streamName expectedVersion events -> 
    //       let a = EventStore.append store
    //       a streamName expectedVersion events
        
    //    let read : Read = fun streamName version ->
    //        let l = EventStore.load store
    //        l streamName version

    //    let aggregateStreamReader (cmd:ICommand) = read (cmd.AggregateId.ToString() |> EventStore.StreamName) startFrom
    //    let aggregateStreamAppender (cmd:ICommand) = asyncAppend append (cmd.AggregateId.ToString() |> EventStore.StreamName)

    //    cmdMeta //TODO to streamName
    //    |> aggregateStreamReader
    //    |> asyncRehydrate defaultState apply
    //    |> fun(x) -> 
    //        async {
    //            let! (version, state) = x
    //            let e = f state cmd
    //            return (version, e)
    //        }
    //    |> aggregateStreamAppender cmdMeta 
    //    |> Async.Ignore  module App =
  