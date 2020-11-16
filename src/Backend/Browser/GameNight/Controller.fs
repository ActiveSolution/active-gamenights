module Backend.Browser.GameNight.Controller

open Giraffe
open Saturn
open Backend.Extensions
open Microsoft.AspNetCore.Http
open Backend
open FsToolkit.ErrorHandling
open Domain
open Common

let toMissingUserError (ValidationError err) = BrowserError.MissingUser err
let getAll (storage: Storage.Service) basePath domain (ctx : HttpContext) : HttpFuncResult =
    taskResult {
        let! gameNights = storage.GetProposedGameNights()
        let! currentUser = ctx.GetUser() |> Result.mapError toMissingUserError
        return 
            gameNights 
            |> List.ofSeq 
            |> Views.proposedGameNightsView currentUser
            |> Browser.Common.View.html basePath domain (ctx.GetUser() |> Result.toOption)
            |> BrowserResponse.Html
    } 
    |> BrowserTaskResult.handle ctx
    
type CreateProposedGameNightDto =
    { Games : string list
      Dates : string list }

let addProposedGameNight basePath domain (ctx : HttpContext) =
    Views.addProposedGameNightView
    |> Browser.Common.View.html basePath domain None
    |> Html
    |> Ok 
    |> BrowserResult.handle ctx
    
let saveProposedGameNight (storage: Storage.Service) (ctx: HttpContext) : HttpFuncResult =
    taskResult {
        let! dto = ctx.BindJsonAsync<CreateProposedGameNightDto>()
        let! user = ctx.GetUser() |> Result.mapError toMissingUserError
        
        let! req = Workflows.GameNights.ProposeGameNightRequest.create (dto.Games, dto.Dates, user) |> Result.mapError BrowserError.Validation
        let gn = Workflows.GameNights.proposeGameNight req
        
        let! _ = storage.SaveProposedGameNight gn
        return (Redirect "/gamenight")
    }
    |> BrowserTaskResult.handle ctx
    
    
let gameController (storage : Storage.Service) (gameNightId: string) =
    let voteController (gameName: string) =
        let saveGameVote (ctx: HttpContext) = 
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError BrowserError.Validation
                let! gameNight = storage.GetProposedGameNight gameNightId |> Async.StartAsTask |> TaskResult.mapError BrowserError.NotFound
                
                let! gameName = gameName |> GameName.create |> Result.mapError BrowserError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
                let updated = Workflows.GameNights.addGameVote req
                
                let! _ = storage.SaveProposedGameNight updated
                return Redirect "/gamenight"
            }
            |> BrowserTaskResult.handle ctx
                
        let deleteGameVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError BrowserError.Validation
                let! gameNight = storage.GetProposedGameNight gameNightId |> Async.StartAsTask |> TaskResult.mapError BrowserError.NotFound
                
                let! gameName = gameName |> GameName.create |> Result.mapError BrowserError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
                let updated = Workflows.GameNights.removeGameVote req
                
                let! _ = storage.SaveProposedGameNight updated
                return Redirect "/gamenight"
                
            } |> BrowserTaskResult.handle ctx
    
        controller {
            create saveGameVote
            delete deleteGameVote
        }
        
    controller {
        subController "/vote" voteController
    }
    
let dateController (storage: Storage.Service) (gameNightId: string) =
    
    let voteController (date: string) =
    
        let saveDateVote (ctx: HttpContext) = 
            taskResult {
                
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError BrowserError.Validation
                let! gameNight = storage.GetProposedGameNight gameNightId |> Async.StartAsTask |> TaskResult.mapError BrowserError.NotFound
                
                let! date = date |> Date.tryParse |> Result.mapError BrowserError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
                let updated = Workflows.GameNights.addDateVote req
                
                let! _ = storage.SaveProposedGameNight updated
                return Redirect "/gamenight"
            }
            |> BrowserTaskResult.handle ctx
                
        let deleteDateVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError BrowserError.Validation
                let! gameNight = storage.GetProposedGameNight gameNightId |> Async.StartAsTask |> TaskResult.mapError BrowserError.NotFound
                
                let! date = date |> Date.tryParse |> Result.mapError BrowserError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
                let updated = Workflows.GameNights.removeDateVote req
                
                let! _ = storage.SaveProposedGameNight updated
                return Redirect "/gamenight"
                
            } |> BrowserTaskResult.handle ctx
    
        controller {
            create saveDateVote
            delete deleteDateVote
        }
        
    controller {
        subController "/vote" voteController
    }
    
let controller (storage: Storage.Service) basePath domain = controller {
    index (getAll storage basePath domain)
    add (addProposedGameNight basePath domain)
    create (saveProposedGameNight storage)
    
    subController "/game" (gameController storage)
    subController "/date" (dateController storage)
}