module Backend.Api.GameNight

open FSharpPlus.Data
open Feliz.ViewEngine
open Giraffe
open Saturn
open Backend.Extensions
open Microsoft.AspNetCore.Http
open Backend
open FsToolkit.ErrorHandling
open Domain
open Feliz.Bulma.ViewEngine
open Backend.Turbo

    
let gameNightsView =
    Bulma.container [
        Html.turboFrame [
            prop.id "confirmed-game-nights"
            prop.src "/confirmedgamenight"
        ]
        Html.turboFrame [
            prop.id "proposed-game-nights"
            prop.src "/proposedgamenight"
        ]
    ]

let toMissingUserError (ValidationError err) = ApiError.MissingUser err
let getAll env : HttpFunc =
    fun ctx ->
        ctx.RespondWithHtml(env, gameNightsView)
    
let gameController env (gameNightId: string) =
    let voteController (gameName: string) =
        let saveGameVote (ctx: HttpContext) = 
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.Validation
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError ApiError.NotFound
                
                let! gameName = gameName |> GameName.create |> Result.mapError ApiError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
                let updated = Workflows.GameNights.addGameVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return "/gamenight"
            }
            |> ctx.RespondWithRedirect
                
        let deleteGameVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.Validation
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError ApiError.NotFound
                
                let! gameName = gameName |> GameName.create |> Result.mapError ApiError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
                let updated = Workflows.GameNights.removeGameVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return "/gamenight"
                
            } |> ctx.RespondWithRedirect
    
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
                
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.Validation
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError ApiError.NotFound
                
                let! date = date |> Date.tryParse |> Result.mapError ApiError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
                let updated = Workflows.GameNights.addDateVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return "/gamenight"
            }
            |> ctx.RespondWithRedirect
                
        let deleteDateVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.Validation
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError ApiError.NotFound
                
                let! date = date |> Date.tryParse |> Result.mapError ApiError.Validation
                let! user = ctx.GetUser() |> Result.mapError toMissingUserError
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
                let updated = Workflows.GameNights.removeDateVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return "/gamenight"
                
            } |> ctx.RespondWithRedirect
    
        controller {
            create saveDateVote
            delete deleteDateVote
        }
        
    controller {
        subController "/vote" voteController
    }
    
let controller env = controller {
    plug [ All ] CommonHttpHandlers.requireUsername
    
    index (getAll env)
    
    subController "/game" (gameController env)
    subController "/date" (dateController env)
}

