module Backend.Api.GameNight

open System
open Giraffe
open Saturn
open Backend.Extensions
open Microsoft.AspNetCore.Http
open Backend
open FsToolkit.ErrorHandling
open Domain
open FSharpPlus.Data


type CreateProposedGameNightDto =
    { Games : string list
      Dates : string list }
    
type GameNightDto =
    { Id : Guid
      GameVotes : (string * (string list)) list
      DateVotes : (DateTime * (string list)) list
      CreatedBy : string }
      
module GameNightDto =
    let private usersToDto (users: Set<User>) =
        users
        |> Set.toList
        |> List.map User.value
    let private gameVotesToDto (votes: NonEmptyMap<GameName, Set<User>>) =
        votes
        |> NonEmptyMap.toList
        |> List.map (fun (game, users) -> game.Val, usersToDto users)
        
    let private dateVotesToDto (votes: NonEmptyMap<Date, Set<User>>) =
        votes
        |> NonEmptyMap.toList
        |> List.map (fun (date, users) -> Date.toDateTime date, usersToDto users)
         
    let fromProposedGameNight (gn : ProposedGameNight) =
        { Id = gn.Id.Val
          GameVotes = gn.GameVotes |> gameVotesToDto
          DateVotes = gn.DateVotes |> dateVotesToDto
          CreatedBy = gn.CreatedBy.Val }
           
    let fromConfirmedGameNight (gn : ConfirmedGameNight) =
        { Id = gn.Id.Val
          GameVotes = gn.GameVotes |> gameVotesToDto
          DateVotes = [ Date.toDateTime gn.Date, usersToDto (gn.Players |> NonEmptySet.toSet) ] 
          CreatedBy = gn.CreatedBy.Val }
    
let parseUsernameHeader (ctx: HttpContext) =
    ctx.TryGetRequestHeader "X-Username"
    |> Result.requireSome (ValidationError "Missing username header")
    |> Result.bind User.create 
    |> Result.mapError ApiError.Validation

module GameNight =
    type GetAllGameNights = unit -> ApiTaskResult<seq<GameNightDto>>
    let getAll env : GetAllGameNights =
        fun () ->
            taskResult {
                let! proposedGameNights = Storage.getAllProposedGameNights env |> Async.map (Seq.map GameNightDto.fromProposedGameNight) |> Async.StartChild
                let! confirmedGameNights = Storage.getAllConfirmedGameNights env |> Async.map (Seq.map GameNightDto.fromConfirmedGameNight) |> Async.StartChild
                let! proposedGameNights = proposedGameNights 
                let! confirmedGameNights = confirmedGameNights 
                return Json (Seq.concat [proposedGameNights; confirmedGameNights])
            } 
        
    let saveProposedGameNight env (ctx: HttpContext) : HttpFuncResult =
        taskResult {
            let! dto = ctx.BindJsonAsync<CreateProposedGameNightDto>()
            let! user = parseUsernameHeader ctx
            
            let! req = Workflows.GameNights.ProposeGameNightRequest.create (dto.Games, dto.Dates, user) |> Result.mapError ApiError.Validation
            let gn = Workflows.GameNights.proposeGameNight req
            
            let! _ = Storage.saveProposedGameNight env gn
            return gn.Id.ToString() |> sprintf "/gamenight/%s" |> Created
        }
        |> ApiTaskResult.handle ctx
    
module Votes =
    let saveGameVote env gameNightId gameName (ctx: HttpContext) = 
        taskResult {
            let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.Validation
            let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError ApiError.NotFound
            
            let! gameName = gameName |> GameName.create |> Result.mapError ApiError.Validation
            let! user = parseUsernameHeader ctx
            let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
            let updated = Workflows.GameNights.addGameVote req
            
            let! _ = Storage.saveProposedGameNight env updated
            return Accepted
        }
        |> ApiTaskResult.handle ctx
            
    let deleteGameVote env gameNightId gameName (ctx: HttpContext) (_: string) =
        taskResult {
            let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.Validation
            let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError ApiError.NotFound
            
            let! gameName = gameName |> GameName.create |> Result.mapError ApiError.Validation
            let! user = ctx.GetUser() |> Result.mapError ApiError.Validation
            let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
            let updated = Workflows.GameNights.removeGameVote req
            
            let! _ = Storage.saveProposedGameNight env updated
            return Accepted
            
        } |> ApiTaskResult.handle ctx
        
        
    let saveDateVote env gameNightId date (ctx: HttpContext) = 
        taskResult {
            
            let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.Validation
            let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError ApiError.NotFound
            
            let! date = date |> Date.tryParse |> Result.mapError ApiError.Validation
            let! user = ctx.GetUser() |> Result.mapError ApiError.Validation
            let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
            let updated = Workflows.GameNights.addDateVote req
            
            let! _ = Storage.saveProposedGameNight env updated
            return Accepted
        }
        |> ApiTaskResult.handle ctx
            
    let deleteDateVote env gameNightId date (ctx: HttpContext) (_: string) =
        taskResult {
            let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.Validation
            let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError ApiError.NotFound
            
            let! date = date |> Date.tryParse |> Result.mapError ApiError.Validation
            let! user = ctx.GetUser() |> Result.mapError ApiError.Validation
            
            let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
            let updated = Workflows.GameNights.removeDateVote req
            
            let! _ = Storage.saveProposedGameNight env updated
            return Accepted
            
        } |> ApiTaskResult.handle ctx
    
    
let controller env =
    let dateController (gameNightId: string) =
        let voteController (date: string) =
        
            controller {
                create (Votes.saveDateVote env gameNightId date)
                delete (Votes.deleteDateVote env gameNightId date)
            }
            
        controller {
            subController "/vote" voteController
        }
        
    let gameController (gameNightId: string) =
        let voteController (gameName: string) =
        
            controller {
                create (Votes.saveGameVote env gameNightId gameName)
                delete (Votes.deleteGameVote env gameNightId gameName)
            }
            
        controller {
            subController "/vote" voteController
        }
    
    controller {
        index (fun ctx -> GameNight.getAll env >> ApiTaskResult.handle ctx)
        create (GameNight.saveProposedGameNight env)
        
        subController "/game" gameController
        subController "/date" dateController
    }