namespace Fiffi.Fx
open Fiffi
open CloudNative.CloudEvents


module Projections =

    let projection(defaultView:'view) (project:'view -> 'event -> 'view) (e:'event seq) =
        Seq.fold project defaultView e

module State =
    let RehydrateStateFrom<'TState> (events: IEvent seq) (defaultState:'TState) f: 'TState =
        Seq.fold f defaultState events

module EventStore =

    type Version =
        | Version of int64

    type StreamName =
        | StreamName of string

    let streamStart =
        0 |> int64 |> Version

    let apply f (Version e) = f e
    let value e = apply id e

    type Load = IEventStore<CloudEvent> -> StreamName -> Version -> Async<Version * CloudEvent seq>

    type Append = IEventStore<CloudEvent> -> StreamName -> Version -> CloudEvent seq -> Async<Version>

    let load : Load = fun store streamName version ->
        async {
            let (Version v) = version
            let (StreamName sn) = streamName
            let! struct(events,version) = store.LoadEventStreamAsync(sn, v) |> Async.AwaitTask
            return (version |> Version, events)
            }

    let append : Append = fun store streamName version events ->
        async {
            let (Version v) = version
            let (StreamName sn) = streamName
            let! version = store.AppendToStreamAsync(sn, v, events |> Seq.toArray) |> Async.AwaitTask
            return version |> Version
        }

    let execute store pub streamName version f : Async<unit> = 
        let fromVersion = 
            match version with
            | Some(x) -> x
            | None -> (int64)0 |> Version
        let read = load store streamName 
        let write = append store streamName
        async {
            let! (v, history) = read fromVersion
            let! domainEvents =  f history
            let! r = write v domainEvents
            let! _ = pub domainEvents |> Async.Ignore
            return r
        } 
        |> Async.Ignore


 
