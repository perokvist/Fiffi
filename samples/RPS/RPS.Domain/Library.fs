module Foo
open Fiffi
open System.Collections.Generic
open System

type GameEnded = 
    { 
    GameId : Guid
    mutable Meta : IDictionary<string, string>
    }
    interface IEvent with
        member x.SourceId = x.GameId.ToString()
        member x.Meta with get() = x.Meta
        member x.Meta with set(v) = x.Meta <- v


