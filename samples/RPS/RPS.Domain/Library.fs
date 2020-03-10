module Domain
open Fiffi
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

type PlayGame = 
    {
        GameId : Guid
        mutable CorrelationId : Guid
        mutable CausationId : Guid
    }
    interface ICommand with
        member x.AggregateId = new AggregateId(x.GameId) :> IAggregateId
        member x.CorrelationId with get() = x.CorrelationId
        member x.CorrelationId with set(v) = x.CorrelationId <- v
        member x.CausationId = x.CausationId
        member x.CausationId with set(v) = x.CausationId <- v

type Hand =
    | None = 0
    | Rock = 10
    | Paper = 20
    | Scissors = 30

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

let DefaultGameState = { GameId = Guid.Empty; Round = 0; Rounds = 0; Players = ({ Id = String.Empty;  Hand = Hand.None }, { Id = String.Empty; Hand = Hand.None} ); Status = GameStatus.NotStarted }

let When (event:IEvent) (state:GameState) : GameState = 
    match event with
    | :? GameStarted -> { state with Status = GameStatus.Created }
    | _ -> state

//let load store aggregateId = 
//    if eventStore.ContainsKey aggregateId
//    then eventStore.[aggregateId]
//    else List.empty

//let persist store id f =
//    load store id |> rehydrate |> f |> append store id

//let playGame (command:PlayGame) (state:GameState) : list<IEvent> =
//    match state. with
//    | 
    



