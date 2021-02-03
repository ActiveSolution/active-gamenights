namespace Domain

open System
open FSharp.UMX
open FsToolkit.ErrorHandling

module GameId =
    let parse (str: string) =
        str
        |> Helpers.tryParseGuid
        |> Result.requireSome (sprintf "%s is not a valid guid" str)
        |> Result.map UMX.tag<GameId>

    let create (guidId: Guid) =
        if guidId = Guid.Empty then
            Error "GameId cannot be empty"
        else
            guidId
            |> UMX.tag<GameId>
            |> Ok

    let newId () = Guid.NewGuid() |> UMX.tag<GameId>
