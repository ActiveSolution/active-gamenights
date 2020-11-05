module Backend.Helpers

open System
open Giraffe
open Microsoft.AspNetCore.Http
open Saturn.ControllerHelpers

let tee f x = f x; x

let notImplemented() = System.NotImplementedException() |> raise

let replaceWhiteSpace (str: string) = str.Replace("__", " ")

let canonize (str: string) = 
    str.Split(" ")
    |> Seq.map (
        Seq.toList >>
        (function
            | first::rest -> Char.ToUpper(first) :: rest
            | [] -> []) >>
        (Array.ofList >> String))
    |> (fun strings -> String.Join(" ", strings))
