module internal Domain.Helpers

open System

let tee f x = f x; x

let notImplemented() = System.NotImplementedException() |> raise

let replaceDelimiter (oldDelim: char) newDelim (str: String) =
    str.Split(oldDelim)
    |> Seq.map (
        Seq.toList >>
        (function
            | first::rest -> Char.ToUpper(first) :: rest
            | [] -> []) >>
        (Array.ofList >> String))
    |> (fun strings -> String.Join(newDelim, strings))

let canonize = replaceDelimiter ' ' "_"
    
let unCanonize = replaceDelimiter '_' " "
    
let tryParseGuid (str: string) =
    match Guid.TryParse str with
    | true, g -> Some g
    | false, _ -> None
    
