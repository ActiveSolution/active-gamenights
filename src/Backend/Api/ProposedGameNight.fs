module Backend.Api.ProposedGameNight

open System
open Giraffe
open FSharpPlus.Data
open Giraffe.ViewEngine
open Infrastructure
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
    let gameCard (gnId: Guid<GameNightId>) (game:Game) votes currentUser actionUrl voteUpdateTarget =
        article [ 
            _class "media" 
            _dataGameName %game.Name
        ] [
            figure [ _class "media-left" ] [ 
                p [ _class "image is-64x64" ] [ img [ _src (game.ImageUrl |> Option.defaultValue "http://via.placeholder.com/64") ]  ] 
            ]
            div [ _class "media-content" ] [
                a [ _href (sprintf "/game/%A" game.Id); _targetTurboFrame "_top" ] [ strong [] [ game.Name |> GameName.toDisplayName |> str ] ]
                nav [ _class "level" ] [ 
                    div [
                        _id (sprintf "proposed-game-night-game-votes-%s-%s" (gnId.ToString()) (game.Id.ToString()))
                        _class "level-left" 
                    ] [
                        yield! GameNightViews.gameVoteButtons gnId game.Id currentUser votes actionUrl voteUpdateTarget
                        if GameNightViews.hasVoted votes currentUser then
                            ()
                        else 
                            GameNightViews.addVoteButton actionUrl voteUpdateTarget
                    ]
                ]
            ]
        ]

    let proposedGameNightView triggerVoteCount (allGames: Map<Guid<GameId>, Game>) currentUser (gn: ProposedGameNight) =
    
        let turboFrameId = "proposed-game-night-" + gn.Id.ToString()
        turboFrame [ _id turboFrameId ] [
            div [
                if triggerVoteCount then Stimulus.controller "refresh-vote-count"
                _class "box mb-5"
                _dataGameNightId (gn.Id.ToString()) 
            ] [
                div [ _class "media" ] [
                    div [ _class "media-content" ] [
                        h5 [ _class "title is-5" ] [ (gn.CreatedBy |> User.toDisplayName) + " wants to play" |> str ]
                        div [ _class "block" ] [
                            for gameId, votes in gn.GameVotes |> NonEmptyMap.toList do
                                let game = allGames.[gameId]
                                let actionUrl = sprintf "/proposedgamenight/%A/game/%A/vote" gn.Id game.Id
                                ul [] [
                                    li [ ] [
                                        gameCard gn.Id game votes currentUser actionUrl turboFrameId
                                    ] 
                                ] 
                        ]
                        div [ _class "block" ] [
                            for date, votes in gn.DateVotes |> NonEmptyMap.toList do
                                let actionUrl = sprintf "/proposedgamenight/%A/date/%s/vote" gn.Id date.AsString
                                ul [] [
                                    li [] [
                                        GameNightViews.dateCard gn.Id date votes currentUser actionUrl turboFrameId
                                    ] 
                                ]
                        ]
                    ]
                ]
            ]
        ]


    let showGameNightView allGames user (gn: ProposedGameNight) =
        section [ _class "section" ] [
            div [ _class "container" ] [
                proposedGameNightView true allGames user gn
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
        
    let gameNightsView allGames currentUser (proposed: List<_>) =
        turboFrame [ _id "proposed-game-nights"] [ 
            match proposed with
            | [] -> 
                section [
                    Stimulus.controller "refresh-vote-count"
                    _class "section"
                ] [ 
                    div [ _class "container"] [ 
                        addProposedGameNightLink
                    ]
                ]
            | proposed ->
                section [
                    Stimulus.controller "refresh-vote-count"
                    _class "section"
                ] [ 
                    div [ _class "container"] [ 
                        h2 [ _class "title is-2" ] [ str "Proposed game nights" ]
                        for gameNight in proposed do proposedGameNightView false allGames currentUser gameNight 
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

    let gameSelect (allGames: seq<Game>) loadGames index (selectedGame: Guid<GameId> option) =
        let placeholder = if index > 1 then "Pick another game" else "Pick a game"
        let fragmentEndpoint =
            selectedGame
            |> Option.map (fun selected -> (sprintf "/fragments/proposedgamenight/gameselect?index=%i&selectedGame=%A" index %selected))
            |> Option.defaultValue (sprintf "/fragments/proposedgamenight/gameselect?index=%i" index)
        div [ _class "field"; _id (sprintf "game-input-%i-field" index) ] [
            div [ _class "control" ] [
                turboFrame [
                    _id (sprintf "game-select-%i" index)
                    if loadGames then _src fragmentEndpoint
                ] [ 
                    div [ _class "select" ] [
                        select [ 
                            _name "Games"
                            _style "min-width: 20em;"
                        ] [
                            option [ _value "none" ] [ str placeholder ]
                            for game in allGames do 
                                option [
                                    _value <| game.Id.ToString()
                                    match selectedGame with
                                    | Some selected when selected = game.Id -> _selected
                                    | _ -> ()
                                ] [ 
                                    game.Name |> GameName.toDisplayName |> str 
                                ]
                        ]
                    ]
                ]
            ]
        ]

    let lazyGameSelect index selectedGame = gameSelect Set.empty true index selectedGame
    let loadedGameSelect allGames index selectedGame = gameSelect allGames false index selectedGame

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
        div [ _class "field"; _id (sprintf "date-input-%i-field" index) ] [
            div [ _class "control" ] [
                input [
                    _id (sprintf "date-input-%i" index)
                    _type "text"
                    _class "input"
                    _name "Dates"
                    _placeholder "yyyy-mm-dd"
                ]
            ]
        ]

    let okDateInput index date =
        div [ _class "field"; _id (sprintf "date-input-%i-field" index) ] [
            div [ _class "control" ] [
                input [
                    _id (sprintf "date-input-%i" index)
                    _type "text"
                    _class "input"
                    _name "Dates"
                    _value (date |> DateTime.asString)
                ]
            ]
        ]

    let errorDateInput index value errorMsg =
        let value = if String.IsNullOrWhiteSpace value then None else Some value
        div [ _class "field"; _id (sprintf "date-input-%i-field" index) ] [
            div [ _class "control" ] [
                input [
                    _id (sprintf "date-input-%i" index)
                    _type "text"
                    _class "input is-danger"
                    _name "Dates"
                    match value with
                    | Some v -> _value v
                    | None -> _placeholder "yyyy-mm-dd"
                ]
            ]
            p [ _class "help is-danger" ] [ str errorMsg ]
        ]

    let private gameInputTitle =
        div [ _class "field" ] [
            div [ _class "control" ] [
                h5 [ _class "title is-5" ] [ str "What do you want to play?"]
            ]
        ]
    let private dateInputTitle =
        div [ _class "field" ] [
            div [ _class "control" ] [
                h5 [ _class "title is-5" ] [ str "When?"]
            ]
        ]

    let addProposedGameNightView isInline game =
        section [ _class "section" ] [
            div [ _class "container" ] [
                h2 [ _class "title is-2" ] [ str "Add proposed game night"]
                turboFrame [ _id "add-proposed-game-night"; _autoscroll ] [ 
                    form [
                        _class "box"
                        _targetTurboFrame (if isInline then "proposed-game-nights" else "_top")
                        _method "POST"
                        _action "/proposedgamenight" 
                    ] [
                        span [
                            _id "game-inputs"
                            _style "display:block; margin-top: 10px;" 
                        ] [
                            gameInputTitle
                            lazyGameSelect 1 game
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
                            Partials.submitButtonWithCancel "add-proposed-game-night-submit-button" "Save" "Cancel" "/fragments/proposedgamenight/addgamenightlink" (if isInline then "add-proposed-game-night" else "_top")
                        else
                            Partials.submitButton "add-proposed-game-night-submit-button" "Save"
                    ]
                ]
            ]
        ]


    let errorGameSelect (allGames: Set<Game>) index errorMsg = 
        let placeholder = if index > 1 then "Pick another game" else "Pick a game"
        div [ _class "field"; _id (sprintf "game-input-%i-field" index) ] [
            div [ _class "control" ] [
                div [ _class "select is-danger" ] [
                    select [ 
                        _name "Games"
                    ] [
                        option [ _value "none" ] [ str placeholder ]
                        for game in allGames do option [ _value <| game.Id.ToString() ] [ game.Name |> GameName.toDisplayName |> str ]
                    ]
                ]
                p [ _class "help is-danger" ] [ str errorMsg ] 
            ]
        ]

    let gameErrorTurboStream existingGames index msg =
        let id = (sprintf "game-input-%i-field" index)
        errorGameSelect existingGames index msg
        |> TurboStream.replace id 

    let dateErrorTurboStream index value msg =
        errorDateInput index value msg
        |> TurboStream.replace (sprintf "date-input-%i-field" index) 

open FsHotWire.Giraffe

[<CLIMutable>]
type CreateProposedGameNightForm =
    { Games : string list
      Dates : string list }
    with
        // todo implement
        member private this.OkInputs submitButtonId = 
            let gameSelects =
                this.Games
                |> List.mapi (fun i g -> 
                    Views.lazyGameSelect (i + 1) (g |> GameId.parse |> Result.toOption) 
                    |> TurboStream.replace (sprintf "game-input-%i-field" (i + 1)) )
                    |> List.append [ TurboStream.replace submitButtonId (Partials.loadingButton submitButtonId "Save") ]
            let dateInputs =
                this.Dates
                |> List.mapi (fun i d -> i, DateTime.tryParse d |> Result.toOption)
                |> List.choose (fun (i,d)-> d |> Option.map (fun date -> i, date))
                |> List.map (fun (index, date) ->
                    Views.okDateInput (index + 1) date
                    |> TurboStream.replace (sprintf "date-input-%i-field" (index + 1)) )
            [ yield! gameSelects 
              yield! dateInputs ]
        member this.FormValidationError submitButtonId errors =
            TurboStream.mergeByTargetId (this.OkInputs submitButtonId) errors
            |> FormValidationError 


module private Validation =
    open Workflows.GameNights
    let validateGames (existingGames: Set<Game>) games =
        let isValid gameIdStr =
            result {
                if gameIdStr = "none" then
                    return! Error "You must pick a game"
                else
                    let! gameId = GameId.parse gameIdStr
                    return! 
                        if existingGames |> Set.map (fun g -> g.Id) |> Set.contains gameId then 
                            Ok gameId
                        else 
                            Error (sprintf "'%s' is not a valid game id" gameIdStr)
            }
        match games with
        | [] -> Error ( Views.gameErrorTurboStream existingGames 1 "You must pick a game" )
        | gs -> gs |> List.mapi (fun i g -> isValid g |> Result.mapError (Views.gameErrorTurboStream existingGames (i + 1))) |> List.sequenceResultM
        |> Result.map (fun gs -> NonEmptySet.create gs.Head gs.Tail)
        |> Result.mapError List.singleton

    let validateDates dates =
        match dates with
        | [] -> Error (Views.dateErrorTurboStream 1 "" "Missing date")
        | ds -> ds |> List.mapi (fun i d -> FutureDate.tryParse d |> Result.mapError (Views.dateErrorTurboStream (i + 1) d)) |> List.sequenceResultM
        |> Result.map (fun ds -> NonEmptySet.create ds.Head ds.Tail)
        |> Result.mapError List.singleton

    let validateCreateGameNightForm user existingGames (form: CreateProposedGameNightForm) : Result<Workflows.GameNights.CreateProposedGameNightRequest, ApiError> =
            
        validation {
            let! games = validateGames existingGames form.Games
            and! dates = validateDates form.Dates

            return 
                { CreateProposedGameNightRequest.CreatedBy = user
                  Games = games
                  Dates = dates }
        }
        |> Result.mapError (form.FormValidationError "add-proposed-game-night-submit-button")

let showProposedGameNight env (ctx: HttpContext) stringId =
    let getData () =
        taskResult {
            let! allGames = Storage.Games.getAllGames env |> Async.map (Game.toMap) |> Async.StartChild
            let! proposed = Storage.GameNights.getAllProposedGameNights env |> Async.StartChild
            let! p = proposed
            let! gs = allGames
            return (p, gs)
        }
    taskResult {
        let! id = GameNightId.parse stringId |> Result.mapError ApiError.BadRequest
        let! proposed, allGames = getData ()
        let gn = proposed |> List.filter (fun gn -> gn.Id = id) |> List.exactlyOne
        let! user = ctx.GetCurrentUser() |> Result.mapError ApiError.MissingUser
        return Views.showGameNightView allGames user.Name gn
    }
    |> (fun view -> ctx.RespondWithHtml(env, Page.GameNights, view))
        
        
let addProposedGameNight env : HttpFunc =
    fun ctx ->
        let isInline = 
            ctx.TryGetQueryStringValue "inline" 
            |> Option.bind bool.tryParse 
            |> Option.defaultValue false
        let gameId =
            ctx.TryGetQueryStringValue "game"
            |> Option.bind (GameId.parse >> Result.toOption)
        Views.addProposedGameNightView isInline gameId 
        |> (fun view -> ctx.RespondWithHtml(env, Page.GameNights, view))

let saveProposedGameNight env (ctx: HttpContext) : HttpFuncResult =
    taskResult {
        let! form = ctx.BindFormAsync<CreateProposedGameNightForm>()
        let! user = ctx.GetCurrentUser() |> Result.mapError ApiError.MissingUser
        let! existingGames = Storage.Games.getAllGames env
        let! req = Validation.validateCreateGameNightForm user.Name (existingGames |> Set.ofSeq) form
        let gn = Workflows.GameNights.createProposedGameNight req
        let! _ = Storage.GameNights.saveProposedGameNight env gn
        return "/proposedgamenight"
            
    } |> ctx.RespondWithRedirect

let getAll env : HttpFunc =
    let getData =
        async {
            let! proposed = Storage.GameNights.getAllProposedGameNights env |> Async.StartChild
            let! allGames = Storage.Games.getAllGames env |> Async.map Game.toMap |> Async.StartChild
            let! p = proposed
            let! gs = allGames
            return (p, gs)
        }
    fun ctx -> 
        taskResult {
            let! (proposed, allGames) = getData
            let! currentUser = ctx.GetCurrentUser() |> Result.mapError ApiError.MissingUser
            return Views.gameNightsView allGames currentUser.Name proposed 
        } 
        |> (fun view -> ctx.RespondWithHtml(env, Page.GameNights, view))
        
let sendGameVoteAddedNotification ctx (gameNightId: Guid<GameNightId>) (gameId: Guid<GameId>) user =
    let votesId = sprintf "proposed-game-night-game-votes-%s-%s" (gameNightId.ToString()) (gameId.ToString())
    let voteId = sprintf "proposed-game-night-game-vote-%s-%s-%s" (gameNightId.ToString()) (gameId.ToString()) %user.Name
    GameNightViews.otherUsersVoteButton voteId user.Name
    |> TurboStream.append votesId 
    |> Seq.singleton
    |> TurboStream.render
    |> RenderView.AsString.htmlNodes
    |> Notifications.sendEvent ctx (Notifications.exceptClient user)
    
let sendDateVoteAddedNotification ctx (gameNightId: Guid<GameNightId>) (date: DateTime) user =
    let votesId = sprintf "proposed-game-night-date-votes-%s-%s" (gameNightId.ToString()) (date.AsString)
    let voteId = sprintf "proposed-game-night-date-vote-%s-%s-%s" (gameNightId.ToString()) (date.AsString) %user.Name
    GameNightViews.otherUsersVoteButton voteId user.Name
    |> TurboStream.append votesId 
    |> Seq.singleton
    |> TurboStream.render
    |> RenderView.AsString.htmlNodes
    |> Notifications.sendEvent ctx (Notifications.exceptClient user)
    
let sendGameVoteRemovedNotification ctx (gameNightId: Guid<GameNightId>) (gameId: Guid<GameId>) user =
    let id = sprintf "proposed-game-night-game-vote-%s-%s-%s" (gameNightId.ToString()) (gameId.ToString()) %user.Name
    TurboStream.remove id 
    |> Seq.singleton
    |> TurboStream.render
    |> RenderView.AsString.htmlNodes
    |> Notifications.sendEvent ctx (Notifications.exceptClient user)
    
let sendDateVoteRemovedNotification ctx (gameNightId: Guid<GameNightId>) (date: DateTime) user =
    let id = sprintf "proposed-game-night-date-vote-%s-%s-%s" (gameNightId.ToString()) (date.AsString) %user.Name
    TurboStream.remove id 
    |> Seq.singleton
    |> TurboStream.render
    |> RenderView.AsString.htmlNodes
    |> Notifications.sendEvent ctx (Notifications.exceptClient user)
        
let gameController env (gameNightId: string) =
    let voteController (gameIdStr: string) =
        let saveGameVote (ctx: HttpContext) = 
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.BadRequest
                let! gameNight = Storage.GameNights.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError (fun _ -> ApiError.NotFound)
                let! gameId = gameIdStr |> GameId.parse |> Result.mapError ApiError.BadRequest
                let! user = ctx.GetCurrentUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameId, user.Name)
                let updated = Workflows.GameNights.addGameVote req
                let! _ = Storage.GameNights.saveProposedGameNight env updated
                
                let! _ = sendGameVoteAddedNotification ctx gameNightId gameId user
                
                return sprintf "/proposedgamenight/%s" (gameNightId.ToString())
            }
            |> ctx.RespondWithRedirect
                
        let deleteGameVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.BadRequest
                let! gameNight = Storage.GameNights.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError (fun _ -> ApiError.NotFound)
                
                let! gameId = gameIdStr |> GameId.parse |> Result.mapError ApiError.BadRequest
                let! user = ctx.GetCurrentUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.GameVoteRequest.create (gameNight, gameId, user.Name)
                let updated = Workflows.GameNights.removeGameVote req
                
                let! _ = Storage.GameNights.saveProposedGameNight env updated
                
                let! _ = sendGameVoteRemovedNotification ctx gameNightId gameId user
                return sprintf "/proposedgamenight/%s" (gameNightId.ToString())
                
            } |> ctx.RespondWithRedirect
    
        controller {
            plug [ All ] CommonHttpHandlers.requireUsername
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
                let! user = ctx.GetCurrentUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user.Name)
                let updated = Workflows.GameNights.addDateVote req
                
                let! _ = Storage.GameNights.saveProposedGameNight env updated
                
                let! _ = sendDateVoteAddedNotification ctx gameNightId date user
                
                return sprintf "/proposedgamenight/%s" (gameNightId.ToString())
            }
            |> ctx.RespondWithRedirect
                
        let deleteDateVote (ctx: HttpContext) (_: string) =
            taskResult {
                let! gameNightId = GameNightId.parse gameNightId |> Result.mapError ApiError.BadRequest
                let! gameNight = Storage.GameNights.getProposedGameNight env gameNightId |> Async.StartAsTask |> TaskResult.mapError (fun _ -> ApiError.NotFound)
                
                let! date = date |> DateTime.tryParse |> Result.mapError ApiError.BadRequest
                let! user = ctx.GetCurrentUser() |> Result.mapError ApiError.MissingUser
                let req = Workflows.GameNights.DateVoteRequest.create (gameNight, date, user.Name)
                let updated = Workflows.GameNights.removeDateVote req
                
                let! _ = Storage.GameNights.saveProposedGameNight env updated
                
                let _ = sendDateVoteRemovedNotification ctx gameNightId date user
                return sprintf "/proposedgamenight/%s" (gameNightId.ToString())
                
            } |> ctx.RespondWithRedirect
    
        controller {
            plug [ All ] CommonHttpHandlers.requireUsername
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
    show (showProposedGameNight env)
    add (addProposedGameNight env)
    create (saveProposedGameNight env)
    
    subController "/game" (gameController env)
    subController "/date" (dateController env)
}

module Fragments =

    let addGameNightLinkFragment env : HttpHandler =
        fun _ ctx -> 
            ctx.RespondWithHtmlFragment(env, Views.addProposedGameNightLink)

    let gameSelectFragment env : HttpHandler =
        fun _ ctx ->
            let index = ctx.TryGetQueryStringValue "index" |> Option.map int |> Option.defaultValue 1

            taskResult {
                let! allGames = Storage.Games.getAllGames env
                let selectedGame = 
                    ctx.TryGetQueryStringValue "selectedGame" 
                    |> Option.bind (GameId.parse >> Result.toOption)
                    |> Option.bind (fun gameId -> 
                        allGames 
                        |> Seq.tryFind (fun g -> g.Id = gameId) 
                        |> Option.map(fun g -> g.Id))

                return Views.loadedGameSelect allGames index selectedGame
            }
            |> (fun view -> ctx.RespondWithHtmlFragment(env, view))

    let addGameSelectFragment : HttpHandler =
        fun _ ctx ->
            let index = ctx.TryGetQueryStringValue "index" |> Option.map int |> Option.defaultValue 1

            [ TurboStream.remove "add-game-button"
              TurboStream.append "game-inputs" (Views.lazyGameSelect index None)
              TurboStream.append "game-inputs" (Views.addGameButton (index + 1)) ]
            |> ctx.RespondWithTurboStream
        
    let addDateInputFragment : HttpHandler =
        fun _ ctx ->
            let index = ctx.TryGetQueryStringValue "index" |> Option.map int |> Option.defaultValue 1

            [ TurboStream.remove "add-date-input-button"
              TurboStream.append "date-inputs" (Views.emptyDateInput index)
              TurboStream.append "date-inputs" (Views.addDateInputButton (index + 1)) ]
            |> ctx.RespondWithTurboStream
