module Tests

open System
open Xunit
open Fiffi.Fx.App
open Fiffi.Fx.Serialization
open Fiffi.Fx.CloudEvents
open System.Text.Json
open System.Text.Json.Serialization

[<Fact>]
let ``Serialize CloudEvent with union`` () =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    let events = 
        GameCreated { GameId = Guid.NewGuid(); Title = "test" }
        |> createCloudEvent
        |> toJson
        |> toEvent
        |> dataAs<Events> options

    match events with
    | GameCreated e -> Assert.Equal("test", e.Title)
    | _ -> raise (new Exception("boom"))
    
