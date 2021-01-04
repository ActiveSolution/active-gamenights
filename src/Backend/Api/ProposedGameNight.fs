module Backend.Api.ProposedGameNight

open Giraffe
open FSharpPlus.Data
open Microsoft.AspNetCore.Http
open Saturn
open FsToolkit.ErrorHandling
open Backend
open Feliz.Bulma.ViewEngine
open Domain
open Feliz.ViewEngine
open FsHotWire.Feliz
open Backend.Api.Shared

    
let proposedGameNightCard currentUser (gn: ProposedGameNight) =
    let turboFrameId = "proposed-game-night-" + gn.Id.AsString
    Html.turboFrame [
        prop.id turboFrameId
        prop.children [
            Bulma.card [
                prop.classes [ "mb-5"; "game-night-card" ]
                prop.dataGameNightId gn.Id
                prop.children [
                    Bulma.cardHeader [
                        Bulma.cardHeaderTitle.p (gn.CreatedBy.Val + " wants to play")
                    ]
                    Bulma.cardContent [
                        for gameName, votes in gn.GameVotes |> NonEmptyMap.toList do
                            let actionUrl = sprintf "/proposedgamenight/%s/game/%s/vote" gn.Id.AsString gameName.Canonized
                            Html.unorderedList [
                                Html.listItem [
                                    GameNightViews.gameCard gameName votes currentUser actionUrl turboFrameId
                                ] 
                            ] 
                        for date, votes in gn.DateVotes |> NonEmptyMap.toList do
                            let actionUrl = sprintf "/proposedgamenight/%s/date/%s/vote" gn.Id.AsString date.AsString
                            Html.unorderedList [
                                Html.listItem [
                                    GameNightViews.dateCard date votes currentUser actionUrl turboFrameId
                                ] 
                            ]
                    ]
                ]
            ]
        ]
    ]
    

let addProposedGameLink =
    Html.turboFrame [
        prop.id "add-proposed-game-night"
        prop.children [
            Html.a [
                prop.id "add-proposed-game-night-link"
                prop.href "/proposedgamenight/add"
                prop.children [ Bulma.Icons.plusIcon; Html.text "Add new game night" ]
            ]
        ]
    ]
    
let gameNightsView currentUser proposed =
    Html.turboFrame [
        prop.id "proposed-game-nights"
        prop.children [
            Bulma.container [
                Bulma.title.h2 "Proposed game nights"
                Bulma.section [
                    prop.children [ for gameNight in proposed do proposedGameNightCard currentUser gameNight ]
                ]
                addProposedGameLink
            ]
        ]
    ]
    
let gameInputView index =
    Bulma.fieldLabelControl "What do you want to play?" [
        Html.input [
            prop.type'.text
            prop.id (sprintf "game-%i" index)
            prop.classes [ "input" ]
            prop.name "Games"
            prop.placeholder "Enter a game"
        ]
    ]
    
let dateInputView index =
    Bulma.fieldLabelControl "When?" [
        Html.input [
            prop.type'.text
            prop.id (sprintf "date-%i" index)
            prop.classes [ "input" ]
            prop.name "Dates"
            prop.placeholder "Pick a date"
        ]
    ]

let addProposedGameNightView =
    let target = "proposed-game-nights"
    Bulma.section [
        Bulma.title.h2 "Add proposed game night"
        Html.turboFrame [
            prop.id "add-proposed-game-night"
            prop.children [
                Html.form [
                    prop.targetTurboFrame target
                    prop.method "POST"
                    prop.action "/proposedgamenight"
                    prop.children [
                        gameInputView 1
                        dateInputView 1
                        Bulma.fieldControl [
                            Bulma.button.button [
                                color.isPrimary
                                prop.type'.submit
                                prop.text "Save"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
    
let proposedGameNightView currentUser (gn: ProposedGameNight) =
    Bulma.container [
        Bulma.title.h2 "Proposed game night"
        Html.turboFrame [
            prop.id (sprintf "proposed-game-night-%s" gn.Id.AsString)
            prop.children [
                proposedGameNightCard currentUser gn
            ]
        ]
    ]

let toMissingUserError (ValidationError err) = ApiError.MissingUser err

let getProposedGameNight env (ctx: HttpContext) stringId =
    taskResult {
        let! id = GameNightId.parse stringId |> Result.mapError ApiError.Validation
        let! user = ctx.GetUser() |> Result.mapError toMissingUserError
        let! gn = Storage.getProposedGameNight env id |> AsyncResult.mapError ApiError.NotFound
        return proposedGameNightView user gn
    }
    |> (fun view -> ctx.RespondWithHtml(env, view))
    
    
let addProposedGameNight env : HttpFunc =
    fun ctx ->
        ctx.RespondWithHtml(env, addProposedGameNightView)

[<CLIMutable>]
type CreateProposedGameNightForm =
    { Games : string list
      Dates : string list }

let saveProposedGameNight env (ctx: HttpContext) : HttpFuncResult =
    taskResult {
        let! dto = ctx.BindFormAsync<CreateProposedGameNightForm>()
        let! user = ctx.GetUser() |> Result.mapError toMissingUserError
        
        let! req = Workflows.GameNights.ProposeGameNightRequest.create (dto.Games, dto.Dates, user) |> Result.mapError ApiError.Validation
        let gn = Workflows.GameNights.proposeGameNight req
        
        let! _ = Storage.saveProposedGameNight env gn
        return "/proposedgamenight"
    }
    |> ctx.RespondWithRedirect
    
let getAll env : HttpFunc =
    fun ctx -> 
        taskResult {
            let! proposed = Storage.getAllProposedGameNights env
            let! currentUser = ctx.GetUser() |> Result.mapError toMissingUserError
            return gameNightsView currentUser proposed 
        } 
        |> (fun view -> ctx.RespondWithHtml(env, view))
        
        
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
                return sprintf "/proposedgamenight/%s" gameNightId.AsString
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
                return sprintf "/proposedgamenight/%s" gameNightId.AsString
                
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
                return sprintf "/proposedgamenight/%s" gameNightId.AsString
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
                return sprintf "/proposedgamenight/%s" gameNightId.AsString
                
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
    show (getProposedGameNight env)
    add (addProposedGameNight env)
    create (saveProposedGameNight env)
    
    subController "/game" (gameController env)
    subController "/date" (dateController env)

}
