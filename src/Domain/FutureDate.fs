module Domain.FutureDate

open System
open Domain

let tryParse (str: string) =
    match DateTime.TryParse str with
    | true, dt -> 
        if dt.Date < DateTime.Today then Error (ValidationError "Future date must be in the future")
        else dt |> Date.fromDateTime |> FutureDate |> Ok
    | false, _ -> Error (ValidationError "Not a date")
    
let toDateTime (FutureDate date) =
    date |> Date.toDateTime

