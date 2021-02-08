namespace Domain

open System
open FSharp.UMX

module GameName =
    let create str =
        if String.IsNullOrWhiteSpace str then
            Error "GameName cannot be empty"
        else
            Helpers.canonize str
            |> UMX.tag<CanonizedGameName> 
            |> Ok
        
    let toDisplayName (name: string<CanonizedGameName>) =
        %name |> Helpers.unCanonize

