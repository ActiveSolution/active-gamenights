module Backend.Browser.GameNight.Controller

open Giraffe
open Saturn
open Backend.Extensions
open Microsoft.AspNetCore.Http
open Backend
open FsToolkit.ErrorHandling
open Domain

let toMissingUserError (ValidationError err) = BrowserError.MissingUser err
let getAll env (ctx : HttpContext) : HttpFuncResult =
    taskResult {
        let! proposed = Storage.getAllProposedGameNights env
        let! confirmed = Storage.getAllConfirmedGameNights env
        let! currentUser = ctx.GetUser() |> Result.mapError toMissingUserError
        return 
            Views.gameNightsView currentUser confirmed proposed
            |> Browser.Common.View.html env (ctx.GetUser() |> Result.toOption)
            |> BrowserResponse.Html
    } 
    |> BrowserTaskResult.handle ctx
    
type CreateProposedGameNightDto =
    { Games : string list
      Dates : string list }

let addProposedGameNight env (ctx : HttpContext) =
    Views.addProposedGameNightView
    |> Browser.Common.View.html env None
    |> Html
    |> Ok 
    |> BrowserResult.handle ctx
    
let saveProposedGameNight env (ctx: HttpContext) : HttpFuncResult =
    taskResult {
        let! dto = ctx.BindJsonAsync<CreateProposedGameNightDto>()
        let! user = ctx.GetUser() |> Result.mapError toMissingUserError
        
        let! req = Workflows.GameNights.ProposeGameNightRequest.create (dto.Games, dto.Dates, user) |> Result.mapError BrowserError.Validation
        let gn = Workflows.GameNights.proposeGameNight req
        
        let! _ = Storage.saveProposedGameNight env gn
        return (Redirect "/gamenight")
    }
    |> BrowserTaskResult.handle ctx
    
    
let gameController env (gameNightId: string) =
    let voteController (gameName: string) =
        let saveGameVote (ctx: HttpContext) = 
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError BrowserError.Validation
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError BrowserError.NotFound
                
                let! gameName = gameName |> GameName.create |> Result.mapError BrowserError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
                let updated = Workflows.GameNights.addGameVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return Redirect "/gamenight"
            }
            |> BrowserTaskResult.handle ctx
                
        let deleteGameVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError BrowserError.Validation
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError BrowserError.NotFound
                
                let! gameName = gameName |> GameName.create |> Result.mapError BrowserError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
                let updated = Workflows.GameNights.removeGameVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return Redirect "/gamenight"
                
            } |> BrowserTaskResult.handle ctx
    
        controller {
            create saveGameVote
            delete deleteGameVote
        }
        
    controller {
        subController "/vote" voteController
    }
    
let dateController env (gameNightId: string) =
    
    let voteController (date: string) =
    
        let saveDateVote (ctx: HttpContext) = 
            taskResult {
                
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError BrowserError.Validation
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError BrowserError.NotFound
                
                let! date = date |> Date.tryParse |> Result.mapError BrowserError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
                let updated = Workflows.GameNights.addDateVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return Redirect "/gamenight"
            }
            |> BrowserTaskResult.handle ctx
                
        let deleteDateVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError BrowserError.Validation
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError BrowserError.NotFound
                
                let! date = date |> Date.tryParse |> Result.mapError BrowserError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
                let updated = Workflows.GameNights.removeDateVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return Redirect "/gamenight"
                
            } |> BrowserTaskResult.handle ctx
    
        controller {
            create saveDateVote
            delete deleteDateVote
        }
        
    controller {
        subController "/vote" voteController
    }
    
let controller env = controller {
    index (getAll env)
    add (addProposedGameNight env)
    create (saveProposedGameNight env)
    
    subController "/game" (gameController env)
    subController "/date" (dateController env)
}