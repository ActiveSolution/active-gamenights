module WebTests.Extensions

open canopy.classic

let name value = sprintf "[name = '%s']" value |> css

let assertSome msg opt =
    match opt with
    | Some _ -> ()
    | None -> failwith msg
    
let assertNone msg opt =
    match opt with
    | Some _ -> failwith msg
    | None -> ()
    
let waitForElementOption elementOpt =
    waitFor (fun _ -> match elementOpt() with Some _ -> true | None -> false)
let displayedOption elementOpt =
    Option.map displayed elementOpt
    |> Option.defaultWith (failwith "element should have been displayed but was None")
    
let notDisplayedOption elementOpt =
    match elementOpt with
    | Some el -> failwithf "element should not have been displayed but was %A" el
    | None -> ()

module XPath =
    let containsText text = sprintf "contains(text(),'%s')" text
    let containsClass klass = sprintf "contains(concat(' ', @class, ' '), ' %s ')" klass