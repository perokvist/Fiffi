namespace RPS
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

    let RehydrateStateFrom<'TState> (events: IEvent list) (defaultState:'TState) f: 'TState =
        List.fold f defaultState events


module FiffiInterop =

    type Version =
        | Version of int64

    // unwrap with continuation
    let apply f (Version e) = f e

    // unwrap directly
    let value e = apply id e

    type Load = IEventStore -> string -> Version -> Async<Version * IEvent list>

    type Append = IEventStore-> string -> Version -> IEvent list -> Async<Version>

    let load : Load = fun store streamName version ->
        async {
            let! struct(events,version) = store.LoadEventStreamAsync(streamName, value version) |> Async.AwaitTask
            return (version |> Version, events |> List.ofSeq )
            }

    let append : Append = fun store streamName version events ->
        async {
            let! version = store.AppendToStreamAsync(streamName, value version, events |> Seq.toArray) |> Async.AwaitTask
            return version |> Version
        }


module Game =

    type Command =
        | CreateGame of CreateGame
        | JoinGame of JoinGame
        | PlayGame of PlayGame

    let handle (state:GameState) (cmd:Command) : IEvent list =
        List.empty

module App =
   
    let mapAsync f a = async { 
        let! v = a
        return (f v)
    }

    type Append = string -> FiffiInterop.Version -> IEvent list -> Async<FiffiInterop.Version>
    type AsyncAppend = string -> Async<FiffiInterop.Version * IEvent list> -> Async<FiffiInterop.Version>
    type Read = string -> FiffiInterop.Version -> Async<FiffiInterop.Version * IEvent list>
    type Handler<'TCommand, 'TState> = 'TCommand -> FiffiInterop.Version * 'TState -> FiffiInterop.Version * IEvent list
    type AsyncHandler<'TCommand, 'TState> = 'TCommand -> Async<FiffiInterop.Version * 'TState> -> Async<FiffiInterop.Version * IEvent list>
    type AsyncRehydrate<'T> = Async<FiffiInterop.Version * IEvent list> -> Async<FiffiInterop.Version * 'T>

    let asyncRehydrate<'TState> (defaultState:'TState) f : AsyncRehydrate<'TState> = 
        fun x -> x |> mapAsync(fun(version, events) -> (version ,(State.RehydrateStateFrom events defaultState f)))
    
    let asyncHandler (h:Handler<ICommand, GameState>) : AsyncHandler<ICommand, GameState> = 
        fun cmd state -> state |> mapAsync(fun(state) -> (h cmd state))

    let store =  InMemoryEventStore()
    let startFrom = (int64 0) |> FiffiInterop.Version
    
    let append : Append = fun streamName expectedVersion events -> 
        let a = FiffiInterop.append store
        a streamName expectedVersion events
    
    let asyncAppend (a:Append) : AsyncAppend = fun streamName t -> 
        let appender = a(streamName)
        async {
         let! (version, events) = t
         return! appender version events
        }

    let read : Read = fun streamName version ->
        let l = FiffiInterop.load store
        l streamName version
    
    let aggregateStreamReader (cmd:ICommand) = read (cmd.AggregateId.ToString()) startFrom
    let aggregateStreamAppender (cmd:ICommand) = asyncAppend append (cmd.AggregateId.ToString())

    let applicationService<'TCommand, 'TState> (defaultState:'TState) (w) (cmd:'TCommand) (cmdMeta:ICommand) (f) : Async<unit> =
        aggregateStreamReader cmdMeta
        |> asyncRehydrate defaultState w
        |> fun(x) -> 
            async {
                let! (version, state) = x
                let e = f state cmd
                return (version, e)
            }
        |> aggregateStreamAppender cmdMeta 
        |> Async.Ignore  

    let dispatch (cmd:Game.Command) : Async<unit> =
        match cmd with
        | Game.CreateGame(c) -> applicationService State.DefaultGameState State.When cmd c Game.handle
        | _ -> async.Return()
    

    //let foo = load
        //if eventStore.ContainsKey aggregateId
        //then eventStore.[aggregateId]
        //else List.empty

//let persist store id f =
//    load store id |> rehydrate |> f |> append store id

//let playGame (command:PlayGame) (state:GameState) : list<IEvent> =
//    match state. with
//    | 
    



