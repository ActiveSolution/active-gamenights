[<AutoOpen>]
module Backend.Extensions

open System
open Feliz.ViewEngine
open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling
open Domain
open Giraffe
open Saturn
open FsHotWire
open FsHotWire.Feliz
open FSharp.UMX


type BasePath with
    member this.Val = this |> fun (BasePath bp) -> bp

module bool =
    let tryParse (input: string) =
        match bool.TryParse input with
        | true, value -> Some value
        | false, _ -> None
        
module HttpContext = 
    let usernameKey = "username"
    
module ApiResultHelpers =
    let handleResult ctx okFunc res : HttpFuncResult =
        match res with
        | Ok value ->
            okFunc value ctx
        | Error (ApiError.BadRequest err) -> 
            Response.badRequest ctx err
        | Error (ApiError.Domain err) -> 
            Response.badRequest ctx err
        | Error (ApiError.MissingUser _) -> 
            Turbo.redirect "/user/add" ctx
        | Error (ApiError.NotFound _) -> 
            Response.notFound ctx ()
        | Error (ApiError.Duplicate)  -> 
            Response.internalError ctx ()
        | Error (ApiError.FormValidationError ts) ->
            match ctx.Request with
            | AcceptTurboStream ->
                TurboStream.writeTurboStreamContent 422 ts ctx
            | _ -> Response.badRequest ctx ""
        
    let fullPageHtml (env: #ITemplateBuilder) content ctx =
        content
        |> env.Templates.FullPage
        |> Controller.html ctx
        
    let fragment (env: #ITemplateBuilder) content ctx =
        content
        |> env.Templates.Fragment
        |> Controller.html ctx
        
[<RequireQualifiedAccess>]
type ApiFormResponse =
    | Redirect of string
    | TurboStream of TurboStream list
    
type HttpContext with
    member this.GetUser() =
        this.Session.GetString(HttpContext.usernameKey)
        |> Option.ofObj
        |> Result.requireSome "Missing user in HttpContext"
        |> Result.bind Username.create
        
    member this.SetUsername(user: string<CanonizedUsername>) =
        this.Session.SetString(HttpContext.usernameKey, %user)
        
    member this.ClearUsername() =
        this.Session.Remove(HttpContext.usernameKey)

    
    member ctx.RespondWithHtmlFragment (env, content) =
        ApiResultHelpers.fragment env content ctx
    member ctx.RespondWithHtml (env, content) =
        ApiResultHelpers.fullPageHtml env content ctx
    member ctx.RespondWithHtml (env, contentResult) =
        contentResult
        |> ApiResultHelpers.handleResult ctx (ApiResultHelpers.fullPageHtml env)
    member ctx.RespondWithHtml (env, contentTaskResult) =
        contentTaskResult
        |> Task.bind (ApiResultHelpers.handleResult ctx (ApiResultHelpers.fullPageHtml env))
    
    member ctx.RespondWithRedirect(location) = Turbo.redirect location ctx
    member ctx.RespondWithRedirect(locationResult) =
        locationResult 
        |> ApiResultHelpers.handleResult ctx (Turbo.redirect)
    member ctx.RespondWithRedirect(locationTaskResult) =
        locationTaskResult
        |> Task.bind (ApiResultHelpers.handleResult ctx (Turbo.redirect))  
    
    member ctx.RespondWithTurboStream ts = TurboStream.writeTurboStreamContent 200 ts ctx
    member ctx.RespondWithTurboStream tsResult =
        tsResult
        |> ApiResultHelpers.handleResult ctx (TurboStream.writeTurboStreamContent 200)
    member ctx.RespondWithTurboStream tsTaskResult =
        tsTaskResult
        |> Task.bind (ApiResultHelpers.handleResult ctx (TurboStream.writeTurboStreamContent 200))
    
    member ctx.Respond formTaskResult =
        let handleFormResponse value ctx =
            match value with
            | ApiFormResponse.Redirect location -> Turbo.redirect location ctx
            | ApiFormResponse.TurboStream ts -> TurboStream.writeTurboStreamContent 200 ts ctx
            
        formTaskResult
        |> Task.bind (ApiResultHelpers.handleResult ctx handleFormResponse)
        
module Map =
    let keys map = map |> Map.toList |> List.map fst
    let values map = map |> Map.toList |> List.map snd
    
    let change key f map =
        Map.tryFind key map
        |> f
        |> function
        | Some v -> Map.add key v map
        | None -> Map.remove key map

    let tryFindWithDefault defaultValue key map =
        map
        |> Map.tryFind key
        |> Option.defaultValue defaultValue
        

module Async =
    let map f xAsync =
        async {
            let! x = xAsync
            return f x
        }

module Result =
    let toOption xResult = 
        match xResult with 
        | Error _ -> None 
        | Ok v -> Some v

module prop =
    let dataGameNightId (id: Guid<GameNightId>) =
        prop.custom ("data-game-night-id", id.ToString())
    let dataGameName (name: string<CanonizedGameName>) =
        prop.custom ("data-game-name", %name)
    let dataDate (date: DateTime) =
        prop.custom ("data-date", date.AsString)
    let addVoteButton =
        prop.custom ("data-add-vote-button", "")
    let removeVoteButton =
        prop.custom ("data-remove-vote-button", "")
