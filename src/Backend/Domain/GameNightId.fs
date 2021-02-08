namespace Domain

open System
open FSharp.UMX
open FsToolkit.ErrorHandling

module GameNightId =
    let parse (str: string) =
        str
        |> Helpers.tryParseGuid
        |> Result.requireSome "Not a valid guid"
        |> Result.map UMX.tag<GameNightId>

    let create (guidId: Guid) =
        if guidId = Guid.Empty then
            Error "GameNightId cannot be empty"
        else
            guidId
            |> UMX.tag<GameNightId>
            |> Ok

    let newId () = Guid.NewGuid() |> UMX.tag<GameNightId>
