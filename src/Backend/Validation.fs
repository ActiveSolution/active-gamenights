module Backend.Validation

type InvalidInput = 
    { Value: string option
      Msg: string
      Id: string }

    
// From https://blog.ploeh.dk/2020/12/28/an-f-demo-of-validation-with-partial-data-round-trip/
module Result =
    // Result<'a       ,(('b -> 'b) * 'c list)> ->
    // Result<'d       ,(('b -> 'b) * 'c list)> ->
    // Result<('a * 'd),(('b -> 'b) * 'c list)>
    let merge x y =
        match x, y with
        | Ok xres, Ok yres -> Ok (xres, yres)
        | Error (f, e1s), Error (g, e2s)  -> Error (f >> g, e2s @ e1s)
        | Error e, Ok _ -> Error e
        | Ok _, Error e -> Error e

module Validations =
    type ValidationBuilder () =
        member _.BindReturn (x, f) = Result.map f x
        // member _.MergeSources (x, y) = Result.merge x y

    [<AutoOpen>]
    module ComputationExpressions =
        let validation = ValidationBuilder ()
