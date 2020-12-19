module Domain.Date

open System
open Domain


let fromDateTime (dt : DateTime) =
    { Year = dt.Year
      Month = dt.Month
      Day = dt.Day }
let today() = DateTime.Today |> fromDateTime

let tryParse (str: string) =
    match DateTime.TryParse(str) with
    | true, dt -> Ok (fromDateTime dt)
    | false, _ -> sprintf "%s is not a valid date" str |> ValidationError |> Error
let toDateTime (date: Date) =
    DateTime(date.Year, date.Month, date.Day)
let toString date = date.Year.ToString("0000") + "-" + date.Month.ToString("00") + "-" + date.Day.ToString("00")

module Operators =
    let (+) (d) (timeSpan: TimeSpan) =
        d
        |> toDateTime
        |> (fun dt -> dt.Add (timeSpan))
        |> fromDateTime
