module Domain.FutureDate

open System
open Domain
open FSharp.UMX

let tryParse (str: string) : Result<DateTime<FutureDate>, string> =
    match DateTime.TryParse str with
    | true, dt -> 
        if dt.Date < DateTime.Today then Error "Future date must be in the future"
        else Ok (% dt)
    | false, _ -> Error "Not a date"
    
