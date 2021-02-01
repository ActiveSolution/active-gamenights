module Backend.Api.Game

open System
open Domain
open FsHotWire.Giraffe
open Giraffe
open Saturn
open Backend.Extensions
open Backend
open FSharp.UMX
open FsToolkit.ErrorHandling
open Backend.Api.Shared
open Backend.Validation
open Microsoft.AspNetCore.Http

module Metadata =
    open Inputs

    let name = Metadata.create("game-name-input", "Name (*)", "Name")
    let imageUrl = Metadata.create("game-image-input", "Thumbnail image url", "ImageUrl")
    let link = Metadata.create ("game-link-input", "External link", "Link")
    let numberOfPlayers = Metadata.create ("game-number-of-players", "Number of players", "NumberOfPlayers")
    let notes = Metadata.create ("game-notes-input", "Notes", "Notes")


[<CLIMutable>]
type CreateGameForm =
    { Name : string
      NumberOfPlayers : string
      Link : string
      ImageUrl : string
      Notes : string }

    with 
        member private this.OkInputs = 
            [ Inputs.okTextInput Metadata.name this.Name |> TurboStream.replace Metadata.name.Id
              Inputs.okTextInput Metadata.imageUrl this.ImageUrl |> TurboStream.replace Metadata.imageUrl.Id
              Inputs.okTextInput Metadata.link this.Link |> TurboStream.replace Metadata.link.Id
              Inputs.okTextInput Metadata.numberOfPlayers this.NumberOfPlayers |> TurboStream.replace Metadata.numberOfPlayers.Id
              Inputs.okTextareaInput Metadata.notes this.Notes |> TurboStream.replace Metadata.notes.Id ]
        member this.FormValidationError(errors) =

            TurboStream.mergeByTargetId this.OkInputs errors
            |> FormValidationError 

module private Views =
    open Giraffe.ViewEngine

    let gameCard isInline (game: Game) =
        let imageStr = game.ImageUrl |> Option.map (fun x -> x.ToString()) |> Option.defaultValue "http://via.placeholder.com/64"
        let notes = match game.Notes with Some n -> str n | None -> emptyText
        let numPlayers = match game.NumberOfPlayers with Some num -> str ("Number of players: " + num) | None -> emptyText
        let link = match game.Link with Some l -> a [ _href l; _target "_blank" ] [ str l ] | None -> emptyText

        turboFrame [ _id ("game-" + %game.Name) ] [
            div [ _class "box mb-5" ] [
                article [ 
                    _class "media" 
                    _dataGameName %game.Name
                ] [
                    figure [ _class "media-left" ] [ 
                        p [ _class "image is-64x64" ] [ img [ _src imageStr ]  ] 
                    ]
                    div [ _class "media-content" ] [
                        p [] [ strong [] [ str (GameName.toDisplayName game.Name) ] ]
                        p [] [ numPlayers ]
                        p [] [ i [] [ notes ] ]
                        p [] [ link ]
                        a [ 
                            _class "button is-primary is-small" 
                            _href (sprintf "/proposedgamenight/add?game=%s" %game.Name)
                            _targetTurboFrame "_top"
                        ] [ 
                            str "I wanna play this!"
                        ]
                    ]
                    div [ _class "media-right" ] [ 
                        a [ 
                            _href (sprintf "/game/%s/edit?inline=%b" %game.Name isInline) 
                            if not isInline then _targetTurboFrame "_top"
                        ] [ str "edit" ] 
                    ]
                ]
            ]
        ]

    let showGameView isInline (game: Game) =
        section [ _class "section" ] [
            div [ _class "container" ] [
                gameCard isInline game
            ]
        ]

    let addGameLink =
        div [ ] [
            turboFrame 
                [ _id "add-game" ] [
                    a [ 
                        _id "add-game-link"
                        _href "/game/add?inline=true" 
                    ] [ 
                        Icons.plusIcon
                        str "Add new game"
                    ]
                ]
            ]

    let games games =
        match games |> Set.toList with
        | [] ->
            section [ _class "section"] [
                div [ _class "container" ] [
                    h2 [ _class "title is-2" ] [ str "No games" ]
                    addGameLink
                ]
            ]
        | games ->
            turboFrame [ _id "games"] [ 
                section [ _class "section"] [ 
                    div [ _class "container"] [ 
                        for game in games do gameCard true game
                        addGameLink
                    ]
                ]
            ]

    let addGameView isInline =
        // let target = if isInline then "games" else "_top"
        section [ _class "section" ] [
            div [ _class "container" ] [
                h2 [ _class "title is-2" ] [ str "Add a new game"]
                turboFrame [ _id "add-game"; _autoscroll ] [ 
                    form [
                        _class "box"
                        _method "POST"
                        _action "/game" 
                        _targetTurboFrame (if isInline then "games" else "_top")
                    ] [
                        Inputs.textInput Metadata.name None
                        Inputs.textInput Metadata.imageUrl None
                        Inputs.textInput Metadata.link None
                        Inputs.textInput Metadata.numberOfPlayers None
                        Inputs.textareaInput Metadata.notes None

                        div [ _class "field" ] [
                            div [ _class "control" ] [
                                if isInline then 
                                    Partials.submitButtonWithCancel "Save" "Cancel" "/fragments/game/addgamelink" (if isInline then "add-game" else "_top") 
                                else
                                    Partials.submitButton "Save"
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let editGameView isInline (game: Game) =
        let id = "game-" + %game.Name
        let target = if isInline then id else "_top"
        section [ _class "section" ] [
            div [ _class "container" ] [
                h2 [ _class "title is-2" ] [ str "Edit game"]
                turboFrame [ _id id; _autoscroll ] [ 
                    article [ _class "box media mb-5" ] [
                        div [ _class "media-content" ] [
                            form [
                                _method "POST"
                                _action (sprintf "/game/%s/edit" %game.Name) 
                            ] [
                                Inputs.textInput Metadata.name (game.Name |> GameName.toDisplayName |> Some)
                                Inputs.textInput Metadata.imageUrl game.ImageUrl
                                Inputs.textInput Metadata.link game.Link
                                Inputs.textInput Metadata.numberOfPlayers game.NumberOfPlayers
                                Inputs.textareaInput Metadata.notes game.Notes

                                div [ _class "field" ] [
                                    div [ _class "control" ] [
                                        Partials.submitButtonWithCancel "Save" "Cancel" (sprintf "/game/%s?inline=%b" %game.Name isInline) target
                                    ]
                                ]
                            ]
                        ]
                        div [ _class "media-right" ] [
                            a [ 
                                  _href (sprintf "/game/%s?inline=%b" %game.Name isInline) 
                                  if not isInline then _targetTurboFrame "_top"
                            ] [ 
                                span [ _class "icon" ] [ 
                                    i [ _class "fas fa-times-circle fa-lg has-text-grey-lighter" ] [ ] 
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

module private Validation =
    let private validateGameName existing gameName : Result<string<CanonizedGameName>, TurboStream list> =
        let validateDuplicate onError items item = if Seq.contains item items then Error onError else Ok item
        gameName
        |> Option.ofString
        |> Result.requireSome "Game name missing"
        |> Result.bind GameName.create
        |> Result.bind (validateDuplicate "A game with this name already exists" existing)
        |> Result.mapError (fun err -> 
            Inputs.errorTextInput Metadata.name gameName err 
            |> TurboStream.replace Metadata.name.Id 
            |> List.singleton)

    let private tryParseUrl errorMsg u = 
        match Uri.TryCreate(u, UriKind.Absolute) with
        | true, _ -> Ok u
        | false, _ -> Error errorMsg

    let private validateLink link : Result<string option, TurboStream list> =
        link
        |> Option.ofString
        |> function 
        | None -> Ok None
        | Some link ->
            link
            |> (tryParseUrl "Not a valid link" >> Result.map Some)
            |> (Result.mapError (fun err ->
                Inputs.errorTextInput Metadata.link link err 
                |> TurboStream.replace Metadata.link.Id 
                |> List.singleton))

    let private validateImageUrl imageUrl : Result<string option, TurboStream list> =
        imageUrl
        |> Option.ofString
        |> function 
        | None -> Ok None
        | Some imageUrl ->
            imageUrl
            |> (tryParseUrl "Not a valid imageUrl" >> Result.map Some)
            |> (Result.mapError (fun err ->
                Inputs.errorTextInput Metadata.imageUrl imageUrl err 
                |> TurboStream.replace Metadata.imageUrl.Id 
                |> List.singleton))

    open Workflows.Game
    let validateCreateGameForm user existingGameNames (form: CreateGameForm) : Result<Workflows.Game.AddGameRequest, ApiError> =
            
        validation {
            let! gameName = validateGameName existingGameNames form.Name
            and! link = validateLink form.Link
            and! imageUrl = validateImageUrl form.ImageUrl
            return 
                { Workflows.Game.AddGameRequest.GameName = gameName
                  CreatedBy = user
                  ImageUrl = imageUrl
                  Link = link
                  Notes = form.Notes |> Option.ofString
                  NumberOfPlayers = form.NumberOfPlayers |> Option.ofString
                  ExistingGames = existingGameNames }
        }
        |> Result.mapError form.FormValidationError

    let validateUpdateGameForm user (form: CreateGameForm) : Result<Workflows.Game.UpdateGameRequest, ApiError> =
            
        validation {
            let! gameName = validateGameName [] form.Name
            and! link = validateLink form.Link
            and! imageUrl = validateImageUrl form.ImageUrl
            return 
                { Workflows.Game.UpdateGameRequest.GameName = gameName
                  CreatedBy = user
                  ImageUrl = imageUrl
                  Link = link
                  Notes = form.Notes |> Option.ofString
                  NumberOfPlayers = form.NumberOfPlayers |> Option.ofString }
        }
        |> Result.mapError form.FormValidationError

let getAll env : HttpFunc =
    fun ctx -> 
        taskResult {
            let! games = Storage.Games.getAllGames env
            return Views.games games 
        }
        |> (fun view -> ctx.RespondWithHtml(env, Page.Games, view))

let showGame env (ctx: HttpContext) gameNameStr =
    taskResult {
        let isInline = 
            ctx.TryGetQueryStringValue "inline" 
            |> Option.bind bool.tryParse 
            |> Option.defaultValue false
        let! gameName = GameName.create gameNameStr |> Result.mapError ApiError.BadRequest
        let! game = Storage.Games.getGame env gameName
        return Views.showGameView isInline game
    }
    |> (fun view -> ctx.RespondWithHtml(env, Page.Games, view))

let addGame env : HttpFunc =
    fun ctx ->
        let isInline = 
            ctx.TryGetQueryStringValue "inline" 
            |> Option.bind bool.tryParse 
            |> Option.defaultValue false
        ctx.RespondWithHtml(env, Page.Games, Views.addGameView isInline)

let editGame env (ctx: HttpContext) gameNameStr =
    taskResult {
        let isInline = 
            ctx.TryGetQueryStringValue "inline" 
            |> Option.bind bool.tryParse 
            |> Option.defaultValue false
        let! gameName = GameName.create gameNameStr |> Result.mapError ApiError.BadRequest
        let! game = Storage.Games.getGame env gameName
        return Views.editGameView isInline game 
    }
    |> (fun view -> ctx.RespondWithHtml(env, Page.Games, view))

let saveGame env (ctx: HttpContext): HttpFuncResult =
    taskResult {
        let! form = ctx.BindFormAsync<CreateGameForm>()
        let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
        let! existingGames = Storage.Games.getAllGames env |> Async.map (Set.map (fun x -> x.Name))
        let! request = Validation.validateCreateGameForm user existingGames form
        let! game = Workflows.Game.addGame request |> Result.mapError ApiError.BadRequest
        let! _ = Storage.Games.addGame env game
        return "/game"
    }
    |> ctx.RespondWithRedirect

let updateGame env (ctx: HttpContext) gameNameStr =
    taskResult {
        let! form = ctx.BindFormAsync<CreateGameForm>()
        let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
        let! existingGames = Storage.Games.getAllGames env |> Async.map (Set.map (fun x -> x.Name))
        let! request = Validation.validateUpdateGameForm user form 
        let game = Workflows.Game.updateGame request 
        let! _ = Storage.Games.addGame env game
        return "/game"
    }
    |> ctx.RespondWithRedirect


let controller env = controller {
    plug [ All ] CommonHttpHandlers.requireUsername
    plug [ Add ] (CommonHttpHandlers.privateCachingWithQueries (TimeSpan.FromHours 24.) [| "*" |])
    
    index (getAll env)
    show (showGame env)
    add (addGame env)
    create (saveGame env)
    edit (editGame env)
    update (updateGame env)
}

module Fragments =
    let addGameLinkFragment env : HttpHandler =
        fun _ ctx -> 
            ctx.RespondWithHtmlFragment(env, Views.addGameLink)
