namespace RPS

open Fiffi
open Fiffi.Fx
open System
open CloudNative.CloudEvents
open System.Text.Json
open System.Text.Json.Serialization
open Fiffi.Fx.App

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
          CorrelationId: Guid
          CausationId: Guid }

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

module Read =

    type GamesQuery() =
        class
        end
    
    type GameQuery = { Id: Guid }

    type GameView =
        { Id: Guid
          Title: string
          Description: string }

    type GameSummaryView = { Id: Guid; Title: string }

    type Query =
        | GameQuery of GameQuery
        | GamesQuery of GamesQuery

    type View =
        | GameView of Option<GameView>
        | GameSummaryView of GameSummaryView seq

    let gameSummaryViewDefault =
        {
            GameSummaryView.Id = Guid.Empty;
            Title = String.Empty
        }

    let gameViewDefault =
        { GameView.Id = Guid.Empty
          Title = String.Empty
          Description = String.Empty }

    let gameView (view: GameView) (event: Game.Events) : GameView =
        match event with
        | Game.GameCreated e -> { view with Title = e.Title }
        | _ -> view

    let gameSummaryView (view: GameSummaryView) (event: Game.Events) : GameSummaryView =
        match event with
        | Game.GameCreated e -> { view with Title = e.Title }
        | _ -> view

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

module ApplicationServices =
    open Game

    let dummyHandle state =
        [ GameStarted
            { GameId = Guid.NewGuid()
              PlayerId = "test" }
          GameCreated
              { GameId = Guid.Empty
                Title = "testing" } ]


    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    let converter =
        CloudEvents.convertToDomainEvent<Events> options

    let createGame (cmd: CreateGame) (history: CloudEvent seq) : Async<CloudEvent seq> =
        async {
            let state =
                Seq.fold State.apply State.DefaultGameState (history |> converter)

            return dummyHandle state |> CloudEvents.toCloudEvents
        }

module App =
    open Game
    open Read
    open ApplicationServices

    let gameQuery (q: GameQuery) (s: Source<Events, GameView>) : GameView =
        match s with
        | Stream x ->
            { Id = Guid.NewGuid()
              Title = "f"
              Description = "f" }
        | Snapshot x -> x

    let gamesQuery (q: GamesQuery) (s: Source<Events, GameSummaryView list>) : GameSummaryView list =
        match s with
        | Stream x -> [ { Id = Guid.NewGuid(); Title = "f" } ]
        | Snapshot x -> x

    let init store pub =
        let snapshot =
            new Fiffi.InMemory.InMemorySnapshotStore()

        let streamExecution = EventStore.execute store pub
        let appExecute = App.execute streamExecution

        let gameStream gameId =
            $"games-{gameId}" |> EventStore.StreamName

        let dispatch cmd : Async<unit> =
            match cmd with
            | CreateGame c ->
                let streamName = c.GameId |> gameStream

                streamName
                |> CloudEvents.meta (c.CorrelationId, c.CausationId, "game", "creategame") //TODO return tuple with stream name
                |> appExecute streamName (createGame c)
            | _ -> async.Return()

        let updates evt : Async<unit> =
            match evt with
            | GameCreated e -> Snapshot.apply<GameView> snapshot (e.GameId |> string) (fun view -> gameView view evt)
            | _ -> async.Return()

        let triggers evt : Async<unit> =
            match evt with
            | _ -> async.Return()

        let eventDispatch evt : Async<unit> = //TODO cloud event with known events
            async {
                updates evt |> ignore
                triggers evt |> ignore
            }

        let queries q : Async<View> =
            match q with
            | Query.GameQuery x ->
                async {
                    let! snapView = Snapshot.get<GameView> snapshot (x.Id |> string)
                    return match snapView with
                            | Option.Some(v) -> View.GameView (Option.Some v.value)
                            | Option.None -> View.GameView Option.None
                }
            | Query.GamesQuery x ->
                async {
                    let! (v, events) = EventStore.load store ("all" |> EventStore.StreamName) EventStore.streamStart
                    let view = 
                        events 
                        |> Seq.groupBy (fun e -> e.Subject)
                        |> Seq.map (fun (k, v) -> (k , ApplicationServices.converter v))
                        |> Seq.map (fun (id, e) -> Projections.projection Read.gameSummaryViewDefault Read.gameSummaryView e)
                    return View.GameSummaryView view
                }
        Fiffi.Fx.App.Module<Command, Events, Query, View>(dispatch, eventDispatch, queries)
