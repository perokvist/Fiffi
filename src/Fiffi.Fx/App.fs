namespace Fiffi.Fx

open Fiffi.CloudEvents
open CloudNative.CloudEvents
open System

type ApplicationService = CloudEvent list -> Async<CloudEvent list>
type StreamExecution = EventStore.StreamName -> (ApplicationService) -> Async<unit>

module Serialization =
    let toJson cloudEvent = Extensions.ToJson cloudEvent

    let toEvent (json: string) = Extensions.ToEvent json

module CloudEvents =

    let attachMetaData (meta: Fiffi.EventMetaData) (f: ApplicationService) : ApplicationService =
        fun x ->
            async {
                let! appServiceResult = f x

                return
                    appServiceResult
                    |> List.map
                        (fun ce ->
                            let et = new EventMetaDataExtension()
                            et.MetaData <- meta
                            et.Attach ce
                            ce)
            }

    let createCloudEvent data : CloudEvent =
        let ext = new EventMetaDataExtension()
        let ext2 = new EventStoreMetaDataExtension()

        let e =
            CloudEvent("test", Uri("urn:test"), null, DateTime.UtcNow, ext, ext2)

        e.Data <- data
        e

        //=> new CloudEvent(CloudEventsSpecVersion.V1_0,
        //@event.GetEventName(),
        //source ?? new Uri($"urn:{@event.GetType().Namespace?.Replace('.', ':').ToLower()}"),
        //@event.GetStreamName(),
        //id: @event.SourceId,
        //time: @event.OccuredAt(),
        //new EventMetaDataExtension { MetaData = @event.Meta.GetEventMetaData() })
        //        {
        //DataContentType = new ContentType(MediaTypeNames.Application.Json),
        //Data = @event.Event
        //        };


    let toCloudEvents domainEvents =
        domainEvents
        |> List.map (fun x -> x |> createCloudEvent)

    let meta (correlation, causation, aggregateName) (streamName: EventStore.StreamName) =
        new Fiffi.EventMetaData(
            correlation,
            causation,
            Guid.NewGuid(),
            (string) streamName,
            aggregateName,
            0 |> int64,
            "",
            DateTime.UtcNow.Ticks
        )

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

    let execute (a: StreamExecution) (s: EventStore.StreamName) (f: ApplicationService) (m: Fiffi.EventMetaData) =
        f |> (m |> CloudEvents.attachMetaData) |> a s
