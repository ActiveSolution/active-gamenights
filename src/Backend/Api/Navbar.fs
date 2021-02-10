module Backend.Api.Navbar

open FSharpPlus.Data
open Giraffe
open Domain
open FsToolkit.ErrorHandling
open Backend.Extensions
open FSharp.UMX
open Giraffe.ViewEngine
open FsHotWire.Giraffe
open Infrastructure
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
                a [ _class "navbar-link"; _id "username" ] [ str (user |> User.toDisplayName) ]
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
        span [ Stimulus.target { Controller = "unvoted-count"; TargetName = "count" } ] [
            if numGN < 1 then
                emptyText
            else
                div [ _class "circle ml-2" ] [ str (string numGN) ]
        ]

    let gameNightsLink unvotedCount isActive =
        a [
            if isActive then _class "navbar-item is-tab is-active" else _class "navbar-item is-tab"
            _href "/gamenight" 
        ] [ 
            str "GameNights"
            unvotedGameNightsCountView unvotedCount
        ]

    let gamesLink isActive =
        a [
            if isActive then _class "navbar-item is-tab is-active" else _class "navbar-item is-tab"
            _href "/game" 
        ] [ 
            str "Games"
        ]

    let navbarView user (page: Page) unvotedCount =
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
                    _class "navbar-start" 
                ] [
                    match user with
                    | Some _ ->
                        gamesLink (page = Page.Games)
                        gameNightsLink unvotedCount (page = Page.GameNights)
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
            let! user = ctx.GetCurrentUser() |> Result.mapError ApiError.MissingUser
            let count = ProposedGameNight.gameNightsWhereUserHasNotVoted allGameNights user.Name
            return Views.unvotedGameNightsCountView count
        }
        |> (fun view -> ctx.RespondWithHtmlFragment(env, view))