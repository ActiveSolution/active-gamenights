namespace Domain

open System
open FsToolkit.ErrorHandling

module GameNightId =
    let value (GameNightId id) = id
    let parse (str: string) =
        str
        |> Helpers.tryParseGuid
        |> Result.requireSome (ValidationError "Not a valid guid")
        |> Result.map GameNightId
    let newId () = Guid.NewGuid() |> GameNightId
    let toString id = (value id).ToString()

[<AutoOpen>]
module GameNightIdExtensions =
    
    type GameNightId with
        member this.Val = GameNightId.value this
        member this.AsString = this.Val.ToString()

