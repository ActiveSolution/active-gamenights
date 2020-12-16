module internal Domain.Helpers

open System

let tee f x = f x; x

let notImplemented() = System.NotImplementedException() |> raise

let canonize (str: string) = 
    str.Split(' ')
    |> Seq.map (
        Seq.toList >>
        (function
            | first::rest -> Char.ToUpper(first) :: rest
            | [] -> []) >>
        (Array.ofList >> String))
    |> (fun strings -> String.Join("_", strings))
    
let tryParseGuid (str: string) =
    match Guid.TryParse str with
    | true, g -> Some g
    | false, _ -> None
    
