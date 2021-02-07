module Backend.Api.Navbar

open FSharpPlus.Data
open Giraffe
open Domain
open FsToolkit.ErrorHandling
open Backend.Extensions
open FSharp.UMX
open Giraffe.ViewEngine
open FsHotWire.Giraffe
open Saturn
open Microsoft.AspNetCore.Http
open Backend

module Views =
    let githubLink =
        a [ 
            _class "navbar-item"
            _href "https://github.com/ActiveSolution/ActiveGameNight/blob/master/CHANGELOG.md"
            _target "blank"
        ] [
            span [ _class "icon" ] [ i [ _class "fab fa-fw fa-github" ] [ ] ]
        ]
        
    let userView (user: string<CanonizedUsername> option) =
        let logoutDropdown (user: string<CanonizedUsername>) =
            div [ _class "navbar-item has-dropdown is-hoverable"; _id "logout-dropdown" ] [
                a [ _class "navbar-link"; _id "username" ] [ str (user |> Username.toDisplayName) ]
                div [ _class "navbar-dropdown" ] [
                    form [
                        _action "/user/logout"
                        _method "POST"
                        _disableTurboDrive
                    ] [
                        input [ 
                            _type "hidden"
                            _name "_method"
                            _value "delete"
                        ]
                        div [ _class "field" ] [
                            div [ _class "control" ] [
                                button [
                                    _class "button is-link is-light has-background-white"  
                                    _id "logout-button"
                                    _type "submit"
                                ] [ str "logout" ]
                            ]
                        ]
                    ]
                ]
            ]
            
        match user with
        | Some u -> logoutDropdown u
        | None -> emptyText

    let unvotedGameNightsCountView (numGN: int) =
        span [ _id "navbar-unvoted-count-span" ] [
            if numGN < 1 then
                emptyText
            else
                div [ _class "circle ml-2" ] [ str (string numGN) ]
        ]

    let private unvotedGameNightsCountViewTF lazyLoad (numGN: int option) =
        turboFrame [
            Stimulus.target { Controller = "unvoted-count"; TargetName = "count" }
            _id "navbar-unvoted-count"
            if lazyLoad then _src "/fragments/navbar/unvotedcount"
        ] [ numGN |> Option.map unvotedGameNightsCountView |> Option.defaultValue emptyText ]
        
    let private gameNightsWhereUserHasNotVoted (allGameNights: ProposedGameNight list) user = 
        allGameNights 
        |> List.choose (fun g -> 
            let gameVoters = g.GameVotes |> NonEmptyMap.values |> Seq.collect Set.toSeq |> Set.ofSeq
            let dateVoters = g.DateVotes |> NonEmptyMap.values |> Seq.collect Set.toSeq |> Set.ofSeq
            let allVoters = gameVoters + dateVoters
            if Set.contains user allVoters then None else Some g.Id)
        |> List.length

    let loadedUnvotedGameNightsCountViewTF (allGameNights: ProposedGameNight list) user =
        unvotedGameNightsCountViewTF false (Some (gameNightsWhereUserHasNotVoted allGameNights user))

    let gameNightsLink isActive =
        a [
            Stimulus.action { DomEvent = "click"; Controller = "active-page"; Action = "toggleClass" }
            Stimulus.target { Controller = "active-page"; TargetName = "element" }
            _targetTurboFrame "content"
            if isActive then _class "navbar-item is-tab is-active" else _class "navbar-item is-tab"
            _href "/gamenight" 
        ] [ 
            str "GameNights"
            unvotedGameNightsCountViewTF true None
        ]

    let gamesLink isActive =
        a [
            Stimulus.action { DomEvent = "click"; Controller = "active-page"; Action = "toggleClass" }
            Stimulus.target { Controller = "active-page"; TargetName = "element" }
            _targetTurboFrame "content"
            if isActive then _class "navbar-item is-tab is-active" else _class "navbar-item is-tab"
            _href "/game" 
        ] [ 
            str "Games"
        ]

    let navbarView user (page: Page) =
        nav [
            _class "navbar is-fixed-top is-info"
            Accessibility._roleNavigation 
            Stimulus.controller "css-class"
            Stimulus.cssClass { Controller = "css-class"; ClassName = "name"; ClassValue = "is-active" }
        ] [
            div [ _class "navbar-brand" ] [
                a [ _class "navbar-item"; _href "/" ] [
                    img [ 
                        _src "/Icons/android-chrome-512x512.png"
                        _alt "Icon"
                        _style "width: 28px; height: 28px;"
                    ]
                    str "Active Game Night"
                ]
                div [ 
                    _id "agn-navbar-burger" 
                    _class "navbar-burger"
                    Stimulus.action { DomEvent = "click"; Controller = "css-class"; Action = "toggleClass" }
                    Stimulus.target { Controller = "css-class"; TargetName = "element" }
                ] [
                    yield! List.replicate 3 (span [] [])
                ]
            ]
            div [ 
                _id "agn-navbar-menu" 
                _class "navbar-menu"
                Stimulus.target { Controller = "css-class"; TargetName = "element" }
            ] [
                div [ 
                    Stimulus.controller "active-page"
                    Stimulus.cssClass { Controller = "active-page"; ClassName = "name"; ClassValue = "is-active" }
                    _class "navbar-start" 
                ] [
                    match user with
                    | Some _ ->
                        gamesLink (page = Page.Games)
                        gameNightsLink (page = Page.GameNights)
                    | None -> 
                        emptyText
                ]
                div [ _class "navbar-end" ] [ githubLink; userView user]
            ]
        ]

let unvotedCountFragment env : HttpHandler =
    fun _ (ctx: HttpContext) ->
        taskResult {
            let! allGameNights = Storage.GameNights.getAllProposedGameNights env
            let! user = ctx.GetUser() |> Result.mapError ApiError.MissingUser
            return Views.loadedUnvotedGameNightsCountViewTF allGameNights user
        }
        |> (fun view -> ctx.RespondWithHtmlFragment(env, view))