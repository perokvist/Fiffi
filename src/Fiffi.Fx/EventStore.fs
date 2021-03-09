namespace Fiffi.Fx
open Fiffi
open CloudNative.CloudEvents


module State =
    let RehydrateStateFrom<'TState> (events: IEvent list) (defaultState:'TState) f: 'TState =
        List.fold f defaultState events

module EventStore =

    type Version =
        | Version of int64

    type StreamName =
        | StreamName of string

    let apply f (Version e) = f e
    let value e = apply id e

    type Load = IEventStore<CloudEvent> -> StreamName -> Version -> Async<Version * CloudEvent list>

    type Append = IEventStore<CloudEvent> -> StreamName -> Version -> CloudEvent list -> Async<Version>

    let load : Load = fun store streamName version ->
        async {
            let (Version v) = version
            let (StreamName sn) = streamName
            let! struct(events,version) = store.LoadEventStreamAsync(sn, v) |> Async.AwaitTask
            return (version |> Version, events |> List.ofSeq )
            }

    let append : Append = fun store streamName version events ->
        async {
            let (Version v) = version
            let (StreamName sn) = streamName
            let! version = store.AppendToStreamAsync(sn, v, events |> Seq.toArray) |> Async.AwaitTask
            return version |> Version
        }

    let execute store pub streamName f : Async<unit> = 
        let read = load store streamName ((int64)0 |> Version)
        let write = append store streamName
        async {
            let! (v, history) = read
            let! domainEvents =  f history
            let! r = write v domainEvents
            let! _ = pub domainEvents |> Async.Ignore
            return r
        } 
        |> Async.Ignore


 
