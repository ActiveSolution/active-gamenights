module Backend.Api.ProposedGameNight

open System
open Giraffe
open FSharpPlus.Data
open Microsoft.AspNetCore.Http
open Saturn
open FsToolkit.ErrorHandling
open Backend
open Domain
open Backend.Api.Shared
open FSharp.UMX
open FsHotWire

module Views =    

    open Giraffe.ViewEngine
    open FsHotWire.Giraffe
    let gameCard (gameName: string<CanonizedGameName>) votes currentUser actionUrl voteUpdateTarget =
        article [ 
            _class "media" 
            _dataGameName %gameName
        ] [
            figure [ _class "media-left" ] [ 
                p [ _class "image is-64x64" ] [ img [ _src "http://via.placeholder.com/64" ]  ] 
            ]
            div [ _class "media-content" ] [
                div [ _class "content" ] [ 
                    p [] [ gameName |> GameName.toDisplayName |> str ]
                ]
                nav [ _class "level" ] [ 
                    div [ _class "level-left" ] [
                        yield! GameNightViews.gameVoteButtons currentUser votes actionUrl voteUpdateTarget
                        if GameNightViews.hasVoted votes currentUser then
                            ()
                        else 
                            GameNightViews.addVoteButton actionUrl voteUpdateTarget
                    ]
                ]
            ]
        ]

    let proposedGameNightView isInline currentUser (gn: ProposedGameNight) =
        let turboFrameId = "proposed-game-night-" + gn.Id.ToString()
        turboFrame [ _id turboFrameId ] [
            div [ _class "box mb-5"; _dataGameNightId (gn.Id.ToString()) ] [
                div [ _class "media" ] [
                    div [ _class "media-content" ] [
                        h5 [ _class "title is-5" ] [ (gn.CreatedBy |> Username.toDisplayName) + " wants to play" |> str ]
                        div [ _class "block" ] [
                            for gameName, votes in gn.GameVotes |> NonEmptyMap.toList do
                                let actionUrl = sprintf "/proposedgamenight/%s/game/%s/vote" (gn.Id.ToString()) %gameName
                                ul [] [
                                    li [ ] [
                                        gameCard gameName votes currentUser actionUrl turboFrameId
                                    ] 
                                ] 
                        ]
                        div [ _class "block" ] [
                            for date, votes in gn.DateVotes |> NonEmptyMap.toList do
                                let actionUrl = sprintf "/proposedgamenight/%s/date/%s/vote" (gn.Id.ToString()) date.AsString
                                ul [] [
                                    li [] [
                                        GameNightViews.dateCard date votes currentUser actionUrl turboFrameId
                                    ] 
                                ]
                        ]
                    ]
                    div [ _class "media-right" ] [ 
                        a [ _href (sprintf "/proposedgamenight/%s/edit?inline=%b" (gn.Id.ToString()) isInline) ] [ str "edit" ] 
                    ]
                ]
            ]
        ]


    let showGameNightView isInline user (gn: ProposedGameNight) =
        section [ _class "section" ] [
            div [ _class "container" ] [
                proposedGameNightView isInline user gn
            ]
        ]

    let addProposedGameNightLink =
        turboFrame 
            [ _id "add-proposed-game-night" ]
            [
                a [ 
                    _id "add-proposed-game-night-link"
                    _href "/proposedgamenight/add?inline=true" 
                ] [ 
                    Icons.plusIcon
                    str "Add new game night"
                ]
            ]
        
    let gameNightsView currentUser proposed =
        turboFrame [ _id "proposed-game-nights"] [ 
            match proposed with
            | [] -> 
                emptyText
            | proposed ->
                section [ _class "section"] [ 
                    div [ _class "container"] [ 
                        h2 [ _class "title is-2" ] [ str "Proposed game nights" ]
                        for gameNight in proposed do proposedGameNightView true currentUser gameNight 
                        addProposedGameNightLink
                    ]
                ]
        ]

    let addGameButton nextIndex =
        div [ _class "field" ] [
            div [ _class "control" ] [
                a [ 
                    _id "add-game-button"
                    _class "button is-link is-outlined is-small"
                    _href (sprintf "fragments/proposedgamenight/addgameselect?index=%i" nextIndex) ] [
                        Icons.plusIcon
                    ]
            ]
        ]

    let gameSelect (allGames: Set<string<CanonizedGameName>>) index = 
        let placeholder = if index > 1 then "Pick another game" else "Pick a game"
        turboFrame [ _id (sprintf "game-select-%i" index) ] [
            div [ _class "select" ] [
                select [ 
                    _id (sprintf "game-input-%i" index)
                    _name "Games"
                ] [
                    option [] [ str placeholder ]
                    for game in allGames do option [] [ game |> GameName.toDisplayName |> str ]
                ]
            ]
        ]

    let gameSelectTurboFrame index =
        let placeholder = if index > 1 then "Pick another game" else "Pick a game"
        div [ _class "field" ] [
            div [ _class "control" ] [
                turboFrame [
                    _id (sprintf "game-select-%i" index)
                    _src (sprintf "/fragments/proposedgamenight/gameselect?index=%i" index)
                ] [ 
                    div [ _class "select" ] [
                        select [ 
                            _id (sprintf "game-input-%i" index)
                            _name "Games"
                        ] [
                            option [] [ str placeholder ]
                        ]
                    ]
                ]
            ]
        ]

    let gameSelectTurboFrame_old index =
        turboFrame [
            _id (sprintf "game-select-%i" index)
            _src (sprintf "/fragments/proposedgamenight/gameselect?index=%i" index)
        ] [ 
            div [ _class "field" ] [
                div [ _class "control" ] [
                    div [ _class "select" ] [
                        select [ 
                            _id (sprintf "game-input-%i" index)
                            _name "Games"
                        ] [
                            option [] [ str "Pick a game" ]
                        ]
                    ]
                ]
            ]
        ]

    let addDateInputButton nextIndex =
        div [ _class "field" ] [
            div [ _class "control" ] [
                a [
                    _id "add-date-input-button"
                    _class "button is-link is-outlined is-small"
                    _href (sprintf "/fragments/proposedgamenight/adddateinput?index=%i" nextIndex)
                ] [ Icons.plusIcon]
            ]
        ]

    let emptyDateInput index =
        let placeholder = if index > 1 then "Pick an additional date" else "Pick a date"
        div [ _class "field" ] [
            div [ _class "control" ] [
                input [
                    _type "text"
                    _id (sprintf "date-input-%i" index)
                    _class "input"
                    _name "Dates"
                    _placeholder placeholder
                ]
            ]
        ]

    let addProposedGameNightView isInline =
        let target = if isInline then "proposed-game-nights" else "_top"
        let gameInputTitle =
            div [ _class "field" ] [
                div [ _class "control" ] [
                    h5 [ _class "title is-5" ] [ str "What do you want to play?"]
                ]
            ]
        let dateInputTitle =
            div [ _class "field" ] [
                div [ _class "control" ] [
                    h5 [ _class "title is-5" ] [ str "When?"]
                ]
            ]

        section [ _class "section" ] [
            div [ _class "container" ] [
                h2 [ _class "title is-2" ] [ str "Add proposed game night"]
                turboFrame [ _id "add-proposed-game-night" ] [ 
                    form [
                        _class "box"
                        _targetTurboFrame target
                        _method "POST"
                        _action "/proposedgamenight" 
                    ] [
                        span [
                            _id "game-inputs"
                            _style "display:block; margin-top: 10px;" 
                        ] [
                            gameInputTitle
                            gameSelectTurboFrame 1
                            addGameButton 2
                        ]
                        span [
                            _id "date-inputs"
                            _style "display:block; margin-top: 10px;" 
                        ] [
                            dateInputTitle
                            emptyDateInput 1
                            addDateInputButton 2
                        ]
                        if isInline then 
                            Partials.submitButtonWithCancel "Save" "Cancel" "/fragments/proposedgamenight/addgamenightlink" target
                        else
                            Partials.submitButton "Save"
                    ]
                ]
            ]
        ]

let getProposedGameNight env (ctx: HttpContext) stringId =
    taskResult {
        let isInline = 
            ctx.TryGetQueryStringValue "inline" 
            |> Option.bind bool.tryParse 
            |> Option.defaultValue false
        let! id = GameNightId.parse stringId |> Result.mapError ApiError.BadRequest
        let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
        let! gn = Storage.GameNights.getProposedGameNight env id |> AsyncResult.mapError (fun _ -> ApiError.NotFound)
        return Views.showGameNightView isInline user gn
    }
    |> (fun view -> ctx.RespondWithHtml(env, view))
        
        
let addProposedGameNight env : HttpFunc =
    fun ctx ->
        let isInline = 
            ctx.TryGetQueryStringValue "inline" 
            |> Option.bind bool.tryParse 
            |> Option.defaultValue false
        Views.addProposedGameNightView isInline 
        |> (fun view -> ctx.RespondWithHtml(env, view))

[<CLIMutable>]
type CreateProposedGameNightForm =
    { Games : string list
      Dates : string list }

let saveProposedGameNight env (ctx: HttpContext) : HttpFuncResult =
    taskResult {
        let! dto = ctx.BindFormAsync<CreateProposedGameNightForm>()
        let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
        let! req = Workflows.GameNights.ProposeGameNightRequest.create (dto.Games, dto.Dates, user) |> Result.mapError BadRequest
        let gn = Workflows.GameNights.proposeGameNight req
        let! _ = Storage.GameNights.saveProposedGameNight env gn
        return "/proposedgamenight"
            
    } |> ctx.RespondWithRedirect
    
let getAll env : HttpFunc =
    fun ctx -> 
        taskResult {
            let! proposed = Storage.GameNights.getAllProposedGameNights env
            let! currentUser = ctx.GetUser() |> Result.mapError ApiError.MissingUser
            return Views.gameNightsView currentUser proposed 
        } 
        |> (fun view -> ctx.RespondWithHtml(env, view))
        
        
let gameController env (gameNightId: string) =
    let voteController (gameName: string) =
        let saveGameVote (ctx: HttpContext) = 
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.BadRequest
                let! gameNight = Storage.GameNights.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError (fun _ -> ApiError.NotFound)
                let! gameName = gameName |> GameName.create |> Result.mapError ApiError.BadRequest
                let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
                let updated = Workflows.GameNights.addGameVote req
                let! _ = Storage.GameNights.saveProposedGameNight env updated
                return sprintf "/proposedgamenight/%s" (gameNightId.ToString())
            }
            |> ctx.RespondWithRedirect
                
        let deleteGameVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.BadRequest
                let! gameNight = Storage.GameNights.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError (fun _ -> ApiError.NotFound)
                
                let! gameName = gameName |> GameName.create |> Result.mapError ApiError.BadRequest
                let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameName, user)
                let updated = Workflows.GameNights.removeGameVote req
                
                let! _ = Storage.GameNights.saveProposedGameNight env updated
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
                
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.BadRequest
                let! gameNight = Storage.GameNights.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError (fun _ -> ApiError.NotFound)
                
                let! date = date |> DateTime.tryParse |> Result.mapError ApiError.BadRequest
                let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
                let updated = Workflows.GameNights.addDateVote req
                
                let! _ = Storage.GameNights.saveProposedGameNight env updated
                return sprintf "/proposedgamenight/%s" (gameNightId.ToString())
            }
            |> ctx.RespondWithRedirect
                
        let deleteDateVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.BadRequest
                let! gameNight = Storage.GameNights.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError (fun _ -> ApiError.NotFound)
                
                let! date = date |> DateTime.tryParse |> Result.mapError ApiError.BadRequest
                let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user)
                let updated = Workflows.GameNights.removeDateVote req
                
                let! _ = Storage.GameNights.saveProposedGameNight env updated
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
    plug [ Add ] (CommonHttpHandlers.privateCaching (TimeSpan.FromHours 24.))
    
    index (getAll env)
    show (getProposedGameNight env)
    add (addProposedGameNight env)
    create (saveProposedGameNight env)
    
    subController "/game" (gameController env)
    subController "/date" (dateController env)
}

module Fragments =
    open Giraffe.ViewEngine
    open FsHotWire.Giraffe

    let addGameNightLinkFragment env : HttpHandler =
        fun _ ctx -> 
            ctx.RespondWithHtmlFragment(env, Views.addProposedGameNightLink)

    let gameSelectFragment env : HttpHandler =
        fun _ ctx ->
            let index = ctx.TryGetQueryStringValue "index" |> Option.map int |> Option.defaultValue 1
            taskResult {
                let! allGames = Storage.Games.getAllGames env
                return Views.gameSelect (allGames |> Set.map (fun g -> g.Name)) index 
            }
            |> (fun view -> ctx.RespondWithHtmlFragment(env, view))

    let addGameSelectFragment env : HttpHandler =
        fun _ ctx ->
            let index = ctx.TryGetQueryStringValue "index" |> Option.map int |> Option.defaultValue 1

            match ctx.Request with
            | AcceptTurboStream ->
                [ TurboStream.remove "add-game-button"
                  TurboStream.append "game-inputs" (Views.gameSelectTurboFrame index)
                  TurboStream.append "game-inputs" (Views.addGameButton (index + 1)) ]
                |> ctx.RespondWithTurboStream
            | _ ->
                let view =
                    span [] [
                        Views.gameSelectTurboFrame index
                        Views.addGameButton (index + 1)
                    ]
                ctx.RespondWithHtmlFragment(env, view)
        
    let addDateInputFragment env : HttpHandler =
        fun _ ctx ->
            let index = ctx.TryGetQueryStringValue "index" |> Option.map int |> Option.defaultValue 1

            match ctx.Request with
            | AcceptTurboStream ->
                [ TurboStream.remove "add-date-input-button"
                  TurboStream.append "date-inputs" (Views.emptyDateInput index)
                  TurboStream.append "date-inputs" (Views.addDateInputButton (index + 1)) ]
                |> ctx.RespondWithTurboStream
            | _ ->
                let view =
                    span [] [
                        Views.emptyDateInput index
                        Views.addDateInputButton (index + 1)
                    ]
                ctx.RespondWithHtmlFragment(env, view)
