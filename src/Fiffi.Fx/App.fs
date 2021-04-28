namespace Fiffi.Fx

open Fiffi.CloudEvents
open CloudNative.CloudEvents
open System
open Fiffi
open System.Net.Mime

type ApplicationService = CloudEvent seq -> Async<CloudEvent seq>
type StreamExecution = EventStore.StreamName -> EventStore.Version option -> (ApplicationService) -> Async<unit>

module Interop =
    let toOption<'T> (m:Fiffi.Maybe<'T>) : Option<'T> =
        m.GetOrElse((fun v -> Option.Some(v)), Option<'T>.None)

module Serialization =
    let toJson cloudEvent = Extensions.ToJson cloudEvent

    let toEvent (json: string) = Extensions.ToEvent json

module CloudEvents =

    type MetaData =
        { Correlation: Guid
          Causation: Guid
          AggregateName: string
          Trigger: string
          StreamName: string }

    let attachMetaData (meta: MetaData) (f: ApplicationService) : ApplicationService =
        fun x ->
            async {
                let! appServiceResult = f x

                return
                    appServiceResult
                    |> Seq.map
                        (fun ce ->
                            let ext = ce.Extension<EventMetaDataExtension>()
                            let extMeta =
                                new EventMetaData(
                                    meta.Correlation,
                                    meta.Causation,
                                    ce.Id |> Guid,
                                    meta.StreamName,
                                    meta.AggregateName,
                                    0 |> int64, //unsupported
                                    meta.Trigger,
                                    ce.Time.Value.Ticks
                                )
                            ce.Subject <- meta.StreamName
                            ext.MetaData <- extMeta
                            ext.Attach ce
                            ce)
            }

    let createCloudEvent data : CloudEvent =
        let e =
            CloudEvent(
                data.GetType().Name,
                new Uri(
                    $"urn:{
                               data
                                   .GetType()
                                   .Namespace.ToLower()
                                   .Replace('.', ':')
                    }"
                ),
                Guid.NewGuid() |> string,
                DateTime.UtcNow,
                new EventMetaDataExtension(),
                new EventStoreMetaDataExtension()
            )

        e.DataContentType <- new ContentType(MediaTypeNames.Application.Json)
        e.Data <- data
        e

    let toCloudEvents domainEvents =
        domainEvents
        |> Seq.map (fun x -> x |> createCloudEvent)

    let meta (correlation, causation, aggregateName, trigger) (streamName: EventStore.StreamName) =
        { Correlation = correlation
          Causation = causation
          StreamName = (string) streamName
          AggregateName = aggregateName
          Trigger = trigger }

    let dataAs<'T> options cloudEvent =
        Extensions.DataAs<'T>(cloudEvent, options)

    let convertToDomainEvent<'T> options cloudEvents =
        cloudEvents
        |> Seq.map (fun x -> x |> dataAs<'T> options)

module Snapshot =

    let get<'snap> (s:ISnapshotStore) (key:string) =
        async {
            let! v = s.Get<'snap>(key) |> Async.AwaitTask
            let o = Interop.toOption v
            return o
        }

    let private applysnapshot<'snap> (s:ISnapshotStore) (key:string) f =
        async {
                let cf = System.Func<'snap,'snap>(f)
                let! x = s.Apply<'snap>(key, cf) |> Async.AwaitTask
                x
              } 
              |> Async.Ignore

    let apply<'snap> (s:ISnapshotStore) (key:string) f =
        applysnapshot<'snap> s key f

module App =

    type Source<'event, 'view> =
        | Stream of 'event list
        | Snapshot of 'view

    type dispatch<'T> = 'T -> Async<unit>
    type query<'T, 'R> = 'T -> Async<'R>

    type Module<'command, 'event, 'query, 'view>(commandDispatch:dispatch<'command>, eventDispatch:dispatch<'event>, query:query<'query,'view>) = 

        member x.Dispatch(c) = commandDispatch c 
        member x.When(e) = eventDispatch e
        member x.Query = query

    let mapAsync f a =
        async {
            let! v = a
            return (f v)
        }

    let execute
        (streamExecution: StreamExecution)
        (streamName: EventStore.StreamName)
        (f: ApplicationService)
        (m: CloudEvents.MetaData)
        =
        f
        |> (m |> CloudEvents.attachMetaData)
        |> streamExecution streamName None
