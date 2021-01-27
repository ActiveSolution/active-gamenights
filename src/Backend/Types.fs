namespace Backend

open Giraffe.ViewEngine
open FSharp.UMX
open Domain

type ApiError =
    | Duplicate 
    | NotFound
    | BadRequest of string
    | Domain of string
    | MissingUser of string
    | FormValidationError of seq<FsHotWire.Giraffe.TurboStream>

type InputState<'T> = Result<'T, string * string>
    
type BasePath = BasePath of string
    with member this.Val = this |> function BasePath basePath -> basePath
type Domain = Domain of string
    with member this.Val = this |> function Domain basePath -> basePath
type ITemplateSettings =
    abstract BasePath : BasePath
    abstract Domain : Domain
type ITemplates =
    abstract FullPage : string<CanonizedUsername> option -> XmlNode seq -> string
    abstract Fragment : XmlNode -> string
type ITemplateBuilder =
    abstract Templates : ITemplates
    