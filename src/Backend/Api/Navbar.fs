module Backend.Api.Navbar

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
                        _targetTurboFrame "_top"
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

    let numberOfGameNightsView load (numGN: int option) =
        let num = numGN |> Option.bind (fun i -> if i > 0 then string i |> Some else None) 
        let minWidth = _style "min-width: 3em;"
        let spinner = 
                nav [ _class "level" ] [
                div [ _class "icon" ] [
                    i [ _class "fas fa-spinner fa-spin" ] [ ]
                ]
            ]
        turboFrame [ 
            _id "navbar-game-nights-link-text"
            if load then _src "/fragments/navbar/numberofgamenights"
        ] [
            match num with
            | Some num ->
                span [ _class "tag is-light is-small ml-2"; minWidth ] [ str num ]
            | None -> 
                span [ _class "tag is-light is-small ml-2"; minWidth ] [ spinner ]
        ]

    let lazyNumberOfGameNightsView = numberOfGameNightsView true None
    let loadedNumberOfGameNightsView numGN = numberOfGameNightsView false (Some numGN)

    let gameNightsLink isActive =
        a [ 
            if isActive then _class "navbar-item is-tab is-active" else _class "navbar-item is-tab"
            _href "/gamenight" 
        ] [ 
            str "GameNights"
            lazyNumberOfGameNightsView
        ]

    let gamesLink isActive =
        a [ 
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
                div [ _class "navbar-start" ] [
                    match user with
                    | Some _ ->
                        yield! 
                            match page with 
                            | Page.GameNights -> 
                                [ gameNightsLink true
                                  gamesLink false ]
                            | Page.Games ->
                                [ gameNightsLink false
                                  gamesLink true ]
                            | Page.User | Page.Version ->   
                                [ gameNightsLink false
                                  gamesLink false ]
                    | None -> 
                        emptyText
                ]
                div [ _class "navbar-end" ] [ githubLink; userView user]
            ]
        ]

let numberOfGameNightsFragment env : HttpHandler =
    fun _ (ctx: HttpContext) ->
        taskResult {
            let! allGameNights = Storage.GameNights.getAllProposedGameNights env
            return Views.loadedNumberOfGameNightsView (allGameNights.Length)
        }
        |> (fun view -> ctx.RespondWithHtmlFragment(env, view))