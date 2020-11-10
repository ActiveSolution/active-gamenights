module Backend.Api.GameNight


open Giraffe
open Saturn
open Backend.Extensions
open Microsoft.AspNetCore.Http
open System
open Backend
open FsToolkit.ErrorHandling

type CreateProposedGameNightDto =
    { Games : string list
      Dates : string list }

let parseUsernameHeader (ctx: HttpContext) =
    ctx.TryGetRequestHeader "X-Username"
    |> Result.requireSome (ValidationError "Missing username header")
    |> Result.bind User.create 
    |> Result.mapError AppError.Validation

module GameNight =
    let getAll (storage: Storage.Service) (ctx : HttpContext) =
        taskResult {
            let! gameNights = storage.GetProposedGameNights()
            return Json gameNights
        } 
        |> ApiTaskResult.handle ctx
        
    let saveProposedGameNight (storage: Storage.Service) (ctx: HttpContext) : HttpFuncResult =
        taskResult {
            let! dto = ctx.BindJsonAsync<CreateProposedGameNightDto>()
            let! gameNames =
                dto.Games
               |> List.choose (fun s -> if String.IsNullOrWhiteSpace(s) then None else Some s)
               |> List.map (GameName.create >> Result.mapError AppError.Validation)
               |> List.distinct
               |> List.sequenceResultM
            let! dates =
                dto.Dates
                |> List.choose (fun s -> if String.IsNullOrWhiteSpace s then None else Some s)
                |> List.map (FutureDate.tryParse >> Result.mapError AppError.Validation)
                |> List.distinct
                |> List.sequenceResultM
            let! user = parseUsernameHeader ctx
            
            let req =
                { CreateProposedGameNightRequest.Games = gameNames
                  Dates = dates
                  ProposedBy = user }
            let gn = Domain.createProposedGameNight req
            
            let! _ = storage.SaveProposedGameNight gn
            return gn.Id.ToString() |> sprintf "/gamenight/%s" |> Created
        }
        |> ApiTaskResult.handle ctx
    
module Votes =
    let saveGameVote (storage : Storage.Service) gameNightId gameName (ctx: HttpContext) = 
        taskResult {
            let! gameNightId = GameNightId.parse gameNightId |> Result.mapError AppError.Validation
            let! gameNight = storage.GetProposedGameNight gameNightId |> Async.StartAsTask |> TaskResult.mapError AppError.NotFound
            
            let! gameName = gameName |> Helpers.replaceWhiteSpace |> GameName.create |> Result.mapError AppError.Validation
            let! user = parseUsernameHeader ctx
            let req = 
                { GameVoteRequest.GameName = gameName
                  GameNight = gameNight
                  User = user }
            let updated = Domain.addGameVote req
            
            let! _ = storage.SaveProposedGameNight updated
            return Accepted
        }
        |> ApiTaskResult.handle ctx
            
    let deleteGameVote (storage: Storage.Service) gameNightId gameName (ctx: HttpContext) (_: string) =
        taskResult {
            let! gameNightId = GameNightId.parse gameNightId |> Result.mapError AppError.Validation
            let! gameNight = storage.GetProposedGameNight gameNightId |> Async.StartAsTask |> TaskResult.mapError AppError.NotFound
            
            let! gameName = gameName |> Helpers.replaceWhiteSpace |> GameName.create |> Result.mapError AppError.Validation
            let! user = ctx.GetUser() |> Result.mapError AppError.Validation
            let req = 
                { GameVoteRequest.GameName = gameName
                  GameNight = gameNight
                  User = user }
            let updated = Domain.removeGameVote req
            
            let! _ = storage.SaveProposedGameNight updated
            return Accepted
            
        } |> ApiTaskResult.handle ctx
        
        
    let saveDateVote (storage: Storage.Service) gameNightId date (ctx: HttpContext) = 
        taskResult {
            
            let! gameNightId = GameNightId.parse gameNightId |> Result.mapError AppError.Validation
            let! gameNight = storage.GetProposedGameNight gameNightId |> Async.StartAsTask |> TaskResult.mapError AppError.NotFound
            
            let! date = date |> Date.tryParse |> Result.mapError AppError.Validation
            let! user = ctx.GetUser() |> Result.mapError AppError.Validation
            let req = 
                { DateVoteRequest.Date = date
                  GameNight = gameNight
                  User = user }
            let updated = Domain.addDateVote req
            
            let! _ = storage.SaveProposedGameNight updated
            return Accepted
        }
        |> ApiTaskResult.handle ctx
            
    let deleteDateVote (storage : Storage.Service) gameNightId date (ctx: HttpContext) (_: string) =
        taskResult {
            let! gameNightId = GameNightId.parse gameNightId |> Result.mapError AppError.Validation
            let! gameNight = storage.GetProposedGameNight gameNightId |> Async.StartAsTask |> TaskResult.mapError AppError.NotFound
            
            let! date = date |> Date.tryParse |> Result.mapError AppError.Validation
            let! user = ctx.GetUser() |> Result.mapError AppError.Validation
            let req = 
                { DateVoteRequest.Date = date
                  GameNight = gameNight
                  User = user }
            let updated = Domain.removeDateVote req
            
            let! _ = storage.SaveProposedGameNight updated
            return Accepted
            
        } |> ApiTaskResult.handle ctx
    
    
let controller (storage : Storage.Service) =
    let dateController (gameNightId: string) =
        let voteController (date: string) =
        
            controller {
                create (Votes.saveDateVote storage gameNightId date)
                delete (Votes.deleteDateVote storage gameNightId date)
            }
            
        controller {
            subController "/vote" voteController
        }
        
    let gameController (gameNightId: string) =
        let voteController (gameName: string) =
        
            controller {
                create (Votes.saveGameVote storage gameNightId gameName)
                delete (Votes.deleteGameVote storage gameNightId gameName)
            }
            
        controller {
            subController "/vote" voteController
        }
    
    controller {
        index (GameNight.getAll storage)
        create (GameNight.saveProposedGameNight storage)
        
        subController "/game" gameController
        subController "/date" dateController
    }