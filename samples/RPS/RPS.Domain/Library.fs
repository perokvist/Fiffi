namespace RPS

open Fiffi
open Fiffi.Fx
open System
open CloudNative.CloudEvents
open System.Text.Json
open System.Text.Json.Serialization

module Game =

    type GameCreated = { GameId: Guid; Title: string }

    type GameStarted = { GameId: Guid; PlayerId: string }

    type GameEnded = { GameId: Guid }

    type RoundTied = { GameId: Guid; Round: int }

    type RoundEnded =
        { GameId: Guid
          Winner: string
          Looser: string
          Round: int }

    type Hand =
        | None
        | Rock
        | Paper
        | Scissors

    type HandShown =
        { GameId: Guid
          PlayerId: string
          Hand: Hand }

    type CreateGame =
        { GameId: Guid
          PlayerId: string
          Title: string
          Rounds: int
          mutable CorrelationId: Guid
          mutable CausationId: Guid }

    type JoinGame =
        { GameId: Guid
          PlayerId: string
          mutable CorrelationId: Guid
          mutable CausationId: Guid }

    type PlayGame =
        { GameId: Guid
          mutable CorrelationId: Guid
          mutable CausationId: Guid }

    type RoundResult =
        | Tied = 0
        | Won = 10
        | Lost = 20

    type Player = { Id: string; Hand: Hand }

    type GameStatus =
        | NotStarted
        | Created
        | Started
        | Endend



    type Events =
        | GameCreated of GameCreated
        | GameStarted of GameStarted

    type Command =
        | CreateGame of CreateGame
        | JoinGame of JoinGame
        | PlayGame of PlayGame

module State =

    type GameState =
        { GameId: Guid
          Round: int
          Rounds: int
          Players: Game.Player * Game.Player
          Status: Game.GameStatus }

    let DefaultGameState =
        { GameId = Guid.Empty
          Round = 0
          Rounds = 0
          Players =
              ({ Id = String.Empty
                 Hand = Game.Hand.None },
               { Id = String.Empty
                 Hand = Game.Hand.None })
          Status = Game.GameStatus.NotStarted }

    let apply (state: GameState) (event: Game.Events) : GameState =
        match event with
        | Game.GameStarted e ->
            { state with
                  Status = Game.GameStatus.Created }
        | _ -> state

module App =
    open Game

    let pub events = async { return 0 } |> Async.Ignore
    let store = Fiffi.CloudEvents.CloudEventStore null
    let streamExecution = EventStore.execute store pub

    let functionThatCallsDomainModel =
        streamExecution ("myStream" |> EventStore.StreamName)

    let dummyHandle state =
        [ GameStarted
            { GameId = Guid.NewGuid()
              PlayerId = "test" }
          GameCreated
              { GameId = Guid.Empty
                Title = "testing" } ]

    let createGame (history: CloudEvent list) : Async<CloudEvent list> =
        async {
            let options = JsonSerializerOptions()
            options.Converters.Add(JsonFSharpConverter())

            let state =
                List.fold
                    State.apply
                    State.DefaultGameState
                    (history
                     |> List.map (fun x -> x |> CloudEvents.dataAs<Events> null))

            return
                dummyHandle state
                |> List.map (fun x -> x |> CloudEvents.createCloudEvent)
        }

    let r = functionThatCallsDomainModel createGame

//let dispatch cmd : Async<unit> =
//    let store = InMemoryEventStore()
//    match cmd with
//    | Game.CreateGame c -> App.applicationService store State.DefaultGameState State.When cmd c Game.handle
//    | Game.JoinGame c -> App.applicationService store State.DefaultGameState State.When cmd c Game.handle
//    | _ -> async.Return()
