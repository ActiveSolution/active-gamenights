namespace Backend

open Giraffe.ViewEngine
open FSharp.UMX
open Domain


type AsyncResult<'TResult, 'TError> = Async<Result<'TResult, 'TError>>

type ApiError =
    | Duplicate 
    | NotFound
    | BadRequest of string
    | Domain of string
    | MissingUser of string
    | FormValidationError of seq<FsHotWire.Giraffe.TurboStream>

[<RequireQualifiedAccess>]
type Page =
    | GameNights
    | Games
    | User
    | Version

type InputState<'T> = Result<'T, string * string>
    
type BasePath = BasePath of string
    with member this.Val = this |> function BasePath basePath -> basePath
type Domain = Domain of string
    with member this.Val = this |> function Domain basePath -> basePath
type ITemplateSettings =
    abstract BasePath : BasePath
    abstract Domain : Domain
type ITemplates =
    abstract FullPage : string<CanonizedUsername> option -> Page -> XmlNode seq -> string
    abstract Fragment : XmlNode -> string
type ITemplateBuilder =
    abstract Templates : ITemplates
    
