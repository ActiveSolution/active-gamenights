namespace Domain

module Username =
    open System
    open FSharp.UMX

    let create str =
        if String.IsNullOrWhiteSpace str then
            Error "User cannot be empty"
        else
            Helpers.canonize str
            |> UMX.tag<CanonizedUsername> 
            |> Ok
        
    let toDisplayName (name: string<CanonizedUsername>) =
        %name |> Helpers.unCanonize
