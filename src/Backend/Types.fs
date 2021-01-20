namespace Backend

open Giraffe.ViewEngine

type ApiError =
    | Duplicate 
    | NotFound
    | BadRequest of string
    | Domain of string
    | MissingUser of string
    | FormValidationError of FsHotWire.Giraffe.TurboStream list

type InputData =
    | Valid of string
    | Invalid of Value: string * ErrorMsg: string
    
type BasePath = BasePath of string
    with member this.Val = this |> function BasePath basePath -> basePath
type Domain = Domain of string
    with member this.Val = this |> function Domain basePath -> basePath
type ITemplateSettings =
    abstract BasePath : BasePath
    abstract Domain : Domain
type ITemplates =
    abstract FullPage : XmlNode seq -> string
    abstract Fragment : XmlNode -> string
type ITemplateBuilder =
    abstract Templates : ITemplates
    