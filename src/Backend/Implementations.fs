[<AutoOpen>]
module Backend.Implementations

open System
open Domain
open FsToolkit.ErrorHandling
open Turbolinks

    
open Saturn
module BrowserResult =
    let handle ctx (res : BrowserResult) =
        match res with
        | Ok (Html template) -> Controller.html ctx template
        | Ok (Redirect uri) -> Turbolinks.redirect ctx uri
        | Error (BrowserError.Validation (ValidationError err)) -> Response.badRequest ctx err
        | Error (BrowserError.Domain (DomainError err)) -> Response.badRequest ctx err
        | Error (BrowserError.MissingUser _) -> Turbolinks.redirect ctx "/user/add"
        | Error (BrowserError.NotFound _) -> Response.notFound ctx ()
        | Error (BrowserError.Duplicate)  -> Response.internalError ctx ()
        

module BrowserTaskResult =
    let handle ctx (res: BrowserTaskResult) =
        Task.bind (BrowserResult.handle ctx) res

module ApiResult =
    let handle ctx (res : ApiResult<_>) =
        match res with
        | Ok (Created uri) -> Response.created ctx uri
        | Ok (Json result) -> Controller.json ctx result
        | Ok (Accepted) -> Response.accepted ctx ()
        | Error (ApiError.Validation (ValidationError err)) -> Response.badRequest ctx err
        | Error (ApiError.Domain (DomainError err)) -> Response.badRequest ctx err
        | Error (ApiError.NotFound _) -> Response.notFound ctx ()
        | Error (ApiError.Duplicate)  -> Response.internalError ctx ()
        

module ApiTaskResult =
    let handle ctx (res: ApiTaskResult<_>) =
        Task.bind (ApiResult.handle ctx) res
        
