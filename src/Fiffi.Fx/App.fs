namespace Fiffi.Fx

open Fiffi.CloudEvents
open CloudNative.CloudEvents
open System
open Fiffi
open System.Net.Mime

type ApplicationService = CloudEvent list -> Async<CloudEvent list>
type StreamExecution = EventStore.StreamName -> (ApplicationService) -> Async<unit>

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
                    |> List.map
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
        |> List.map (fun x -> x |> createCloudEvent)

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
        |> List.map (fun x -> x |> dataAs<'T> options)

module App =

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
        |> streamExecution streamName
