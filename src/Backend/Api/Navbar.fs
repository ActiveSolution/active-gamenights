module Backend.Api.Navbar

open Giraffe
open Domain
open FsToolkit.ErrorHandling
open Backend.Extensions
open FSharp.UMX
open Giraffe.ViewEngine
open FsHotWire.Giraffe

let private githubLink =
    a [ 
        _class "navbar-item"
        _href "https://github.com/ActiveSolution/ActiveGameNight/blob/master/CHANGELOG.md"
        _target "blank"
    ] [
        span [ _class "icon" ] [ i [ _class "fab fa-fw fa-github" ] [ ] ]
    ]
    
let private userView (user: string<CanonizedUsername> option) =
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

let navbarView user =
    nav [ _class "navbar is-info"; Accessibility._roleNavigation ] [
        div [ _class "navbar-brand" ] [
            a [ _class "navbar-item"; _href "/" ] [
                img [ 
                    _src "/Icons/android-chrome-512x512.png"
                    _alt "Icon"
                    _style "width: 28px; height: 28px;"
                ]
                str "Active Game Night"
            ]
            div [ _class "navbar-burger"; _id "agn-navbar-burger" ] [
                yield! List.replicate 3 (span [] [])
            ]
        ]
        div [ _class "navbar-menu"; _id "agn-navbar-menu" ] [
            div [ _class "navbar-start" ] [
                match user with
                | Some _ ->
                    a [ _class "navbar-item"; _href "/game" ] [ str "Games"]
                    a [ _class "navbar-item"; _href "/gamenight" ] [ str "GameNights"]
                | None -> 
                    emptyText
            ]
            div [ _class "navbar-end" ] [ githubLink; userView user]
        ]
    ]