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
    let newId () = Guid.NewGuid() |> UMX.tag<GameNightId>
