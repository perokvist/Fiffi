namespace RPS
open Fiffi
open Fiffi.Fx
open System.Collections.Generic
open System


type GameStarted =
    {
        GameId : Guid
        PlayerId : string
        mutable Meta : IDictionary<string, string>
    }
    interface IEvent with
        member x.SourceId = x.GameId.ToString()
        member x.Meta with get() = x.Meta
        member x.Meta with set(v) = x.Meta <- v

type GameEnded = 
    { 
    GameId : Guid
    mutable Meta : IDictionary<string, string>
    }
    interface IEvent with
        member x.SourceId = x.GameId.ToString()
        member x.Meta with get() = x.Meta
        member x.Meta with set(v) = x.Meta <- v

 type RoundTied =
    {
    GameId : Guid
    Round : int
    mutable Meta : IDictionary<string, string>
    }
    interface IEvent with
        member x.SourceId = x.GameId.ToString()
        member x.Meta with get() = x.Meta
        member x.Meta with set(v) = x.Meta <- v

type RoundEnded =
    {
    GameId : Guid
    Winner : string
    Looser : string
    Round : int
    mutable Meta : IDictionary<string, string>
    }

type Hand =
    | None
    | Rock
    | Paper
    | Scissors

type HandShown = 
    {
    GameId : Guid
    PlayerId : string
    Hand : Hand
    mutable Meta : IDictionary<string, string>
    }
    interface IEvent with
        member x.SourceId = x.GameId.ToString()
        member x.Meta with get() = x.Meta
        member x.Meta with set(v) = x.Meta <- v

type CreateGame =
    {
        GameId : Guid
        PlayerId : string
        Title : string
        Rounds : int
        mutable CorrelationId : Guid
        mutable CausationId : Guid
    }
    interface ICommand with
           member x.AggregateId = AggregateId(x.GameId) :> IAggregateId
           member x.CorrelationId with get() = x.CorrelationId
           member x.CorrelationId with set(v) = x.CorrelationId <- v
           member x.CausationId = x.CausationId
           member x.CausationId with set(v) = x.CausationId <- v

type JoinGame =
    {
        GameId : Guid
        PlayerId : string
        mutable CorrelationId : Guid
        mutable CausationId : Guid
    }
    interface ICommand with
        member x.AggregateId = AggregateId(x.GameId) :> IAggregateId
        member x.CorrelationId with get() = x.CorrelationId
        member x.CorrelationId with set(v) = x.CorrelationId <- v
        member x.CausationId = x.CausationId
        member x.CausationId with set(v) = x.CausationId <- v

type PlayGame = 
    {
        GameId : Guid
        mutable CorrelationId : Guid
        mutable CausationId : Guid
    }
    interface ICommand with
        member x.AggregateId = AggregateId(x.GameId) :> IAggregateId
        member x.CorrelationId with get() = x.CorrelationId
        member x.CorrelationId with set(v) = x.CorrelationId <- v
        member x.CausationId = x.CausationId
        member x.CausationId with set(v) = x.CausationId <- v

type RoundResult =
    | Tied = 0
    | Won = 10
    | Lost = 20

type Player =
    {
        Id : string
        Hand : Hand
    }

type GameStatus =
    | NotStarted
    | Created
    | Started
    | Endend

type GameState =
    {
    GameId : Guid
    Round : int
    Rounds : int
    Players : Player * Player
    Status : GameStatus
    }

    

module State =
    let DefaultGameState = { GameId = Guid.Empty; Round = 0; Rounds = 0; Players = ({ Id = String.Empty;  Hand = Hand.None }, { Id = String.Empty; Hand = Hand.None} ); Status = GameStatus.NotStarted }

    let When (state:GameState) (event:IEvent) : GameState = 
        match event with
        | :? GameStarted -> { state with Status = GameStatus.Created }
        | _ -> state

module Game =

    type Command =
        | CreateGame of CreateGame
        | JoinGame of JoinGame
        | PlayGame of PlayGame

    let handle (state:GameState) (cmd:Command) : IEvent list =
        List.empty

module App =

    let dispatch cmd : Async<unit> =
        let store = InMemoryEventStore()
        match cmd with
        | Game.CreateGame(c) -> App.applicationService store State.DefaultGameState State.When cmd c Game.handle
        | Game.JoinGame(c) -> App.applicationService store State.DefaultGameState State.When cmd c Game.handle
        | _ -> async.Return()

  



