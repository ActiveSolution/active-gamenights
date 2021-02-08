namespace Domain


open System

module DateTime =
    let tryParse (str: string) : Result<DateTime, string> =
        match DateTime.TryParse(str) with
        | true, dt -> Ok dt
        | false, _ -> sprintf "%s is not a valid date" str |> Error
    let asString (date: DateTime) = date.ToString("yyyy-MM-dd")

[<AutoOpen>]
module DateExtensions =
    type DateTime with    
        member this.AsString = this |> DateTime.asString
