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
open FSharp.UMX
open FsHotWire

    
let proposedGameNightCard currentUser (gn: ProposedGameNight) =
    let turboFrameId = "proposed-game-night-" + gn.Id.ToString()
    Html.turboFrame [
        prop.id turboFrameId
        prop.children [
            Bulma.card [
                prop.classes [ "mb-5" ]
                prop.dataGameNightId gn.Id
                prop.children [
                    Bulma.cardHeader [
                        Bulma.cardHeaderTitle.p ((gn.CreatedBy |> Username.toDisplayName) + " wants to play")
                    ]
                    Bulma.cardContent [
                        for gameName, votes in gn.GameVotes |> NonEmptyMap.toList do
                            let actionUrl = sprintf "/proposedgamenight/%s/game/%s/vote" (gn.Id.ToString()) %gameName
                            Html.unorderedList [
                                Html.listItem [
                                    GameNightViews.gameCard gameName votes currentUser actionUrl turboFrameId
                                ] 
                            ] 
                        for date, votes in gn.DateVotes |> NonEmptyMap.toList do
                            let actionUrl = sprintf "/proposedgamenight/%s/date/%s/vote" (gn.Id.ToString()) date.AsString
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
    
let private addGameInputButton nextIndex =
    Html.a [
        prop.id "add-game-input"
        prop.href (sprintf "/proposedgamenight/fragment/addgame?index=%i" nextIndex)
        prop.children [
            Html.span [
                Bulma.Icons.plusIcon
                Html.text " add another game"
            ]
        ]
    ]
    
let gameInputView index =
    Html.div [
        Bulma.fieldLabelControl "What do you want to play?" [
            Html.input [
                prop.type'.text
                prop.id (sprintf "game-%i" index)
                prop.classes [ "input" ]
                prop.name "Games"
                prop.placeholder "Enter a game"
            ]
        ]
        addGameInputButton (index + 1)
    ]
    
let private addDateInputButton nextIndex =
    Html.a [
        prop.id "add-date-input"
        prop.href (sprintf "/proposedgamenight/fragment/adddate?index=%i" nextIndex)
        prop.children [
            Html.span [
                Bulma.Icons.plusIcon
                Html.text " add another date"
            ]
        ]
    ]

let dateInputView index =
    Html.div [
        Bulma.fieldLabelControl "When?" [
            Html.input [
                prop.type'.text
                prop.id (sprintf "date-%i" index)
                prop.classes [ "input" ]
                prop.name "Dates"
                prop.placeholder "Pick a date"
            ]
        ]
        addDateInputButton (index + 1)
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
            prop.id (sprintf "proposed-game-night-%s" (gn.Id.ToString()))
            prop.children [
                proposedGameNightCard currentUser gn
            ]
        ]
    ]

let getProposedGameNight env (ctx: HttpContext) stringId =
    taskResult {
        let! id = GameNightId.parse stringId |> Result.mapError ApiError.Validation
        let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
        let! gn = Storage.getProposedGameNight env id |> AsyncResult.mapError (fun _ -> ApiError.NotFound)
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
        let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
        
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
            let! currentUser = ctx.GetUser() |> Result.mapError ApiError.MissingUser
            return gameNightsView currentUser proposed 
        } 
        |> (fun view -> ctx.RespondWithHtml(env, view))
        
        
let gameController env (gameNightId: string) =
    let voteController (gameName: string) =
        let saveGameVote (ctx: HttpContext) = 
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.Validation
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError (fun _ -> ApiError.NotFound)
                
                let! gameName = gameName |> GameName.create |> Result.mapError ApiError.Validation
                let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
                let updated = Workflows.GameNights.addGameVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return sprintf "/proposedgamenight/%s" (gameNightId.ToString())
            }
            |> ctx.RespondWithRedirect
                
        let deleteGameVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.Validation
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError (fun _ -> ApiError.NotFound)
                
                let! gameName = gameName |> GameName.create |> Result.mapError ApiError.Validation
                let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
                let updated = Workflows.GameNights.removeGameVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return sprintf "/proposedgamenight/%s" (gameNightId.ToString())
                
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
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError (fun _ -> ApiError.NotFound)
                
                let! date = date |> DateTime.tryParse |> Result.mapError ApiError.Validation
                let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
                let updated = Workflows.GameNights.addDateVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return sprintf "/proposedgamenight/%s" (gameNightId.ToString())
            }
            |> ctx.RespondWithRedirect
                
        let deleteDateVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.Validation
                let! gameNight = Storage.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError (fun _ -> ApiError.NotFound)
                
                let! date = date |> DateTime.tryParse |> Result.mapError ApiError.Validation
                let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
                let updated = Workflows.GameNights.removeDateVote req
                
                let! _ = Storage.saveProposedGameNight env updated
                return sprintf "/proposedgamenight/%s" (gameNightId.ToString())
                
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

let addGameInputFragment env : HttpHandler =
    fun _ ctx ->
        let index = ctx.TryGetQueryStringValue "index" |> Option.map (fun (i:string) -> int i) |> Option.defaultValue 1
        let inputView = gameInputView index

        match ctx.Request with
        | AcceptTurboStream ->
            inputView
            |> TurboStream.replace "add-game-input"
            |> List.singleton
            |> ctx.RespondWithTurboStream
        | _ ->
            ctx.RespondWithHtmlFragment(env, inputView)
    
    
let addDateInputFragment env : HttpHandler =
    fun _ ctx ->
        let index = ctx.TryGetQueryStringValue "index" |> Option.map (fun (i:string) -> int i) |> Option.defaultValue 1
        let inputView = dateInputView index

        match ctx.Request with
        | AcceptTurboStream ->
            inputView
            |> TurboStream.replace "add-date-input"
            |> List.singleton
            |> ctx.RespondWithTurboStream
        | _ ->
            ctx.RespondWithHtmlFragment(env, inputView)
    
