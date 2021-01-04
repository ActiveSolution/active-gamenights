module Backend.Api.Navbar

open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine
open Giraffe
open Domain
open FsToolkit.ErrorHandling
open Backend.Extensions
open FsHotWire.Feliz
        
let private githubLink =
    Bulma.navbarItem.a [
        prop.href "https://github.com/ActiveSolution/ActiveGameNight/blob/master/CHANGELOG.md"
        prop.target.blank
        prop.children [
            Bulma.icon [
                prop.children [
                    Html.i [
                        prop.classes [ "fab fa-fw fa-github" ]
                    ]
                ]
            ]
        ]
    ]
    
let private userView (user: User option) =
    let logoutDropdown (user: User) =
        Bulma.navbarItem.div [
            navbarItem.hasDropdown
            navbarItem.isHoverable
            prop.id "logout-dropdown"
            prop.children [
                Bulma.navbarLink.a [
                    navbarLink.isArrowless
                    prop.text user.Val
                    prop.id "username"
                ] 
                Bulma.navbarDropdown.div [
                    Html.form [
                        prop.action "/user/logout"
                        prop.method "post"
                        prop.targetTurboFrame.top
                        prop.children [
                            Html.input [
                                prop.type'.hidden
                                prop.name "_method"
                                prop.value "delete"
                            ]
                            Bulma.field.div [
                                prop.children [
                                    Bulma.control.div [
                                        Bulma.button.button [
                                            prop.id "logout-button"
                                            color.isLink
                                            color.isLight
                                            color.hasBackgroundWhite
                                            prop.text "logout"
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
        
    match user with
    | Some u -> logoutDropdown u
    | None -> Html.none
    
let private navbarView user =
    Html.turboFrame [
        prop.id "navbar"
        prop.children [
            Bulma.navbar [
                color.isInfo
                prop.children [ 
                    Bulma.navbarBrand.div [
                        prop.children [
                            Bulma.navbarItem.a [
                                prop.href "/"
                                prop.children [
                                    Html.img [
                                        prop.src "/Icons/android-chrome-512x512.png"
                                        prop.alt "Icon"
                                        prop.style [ style.width (length.px 28); style.height (length.px 28)]
                                    ]
                                    Html.text "Active Game Night"
                                ]
                            ]            
                            Bulma.navbarBurger [
                                prop.id "agn-navbar-burger"
                                navbarItem.hasDropdown
                                prop.children [ yield! List.replicate 3 (Html.span []) ] 
                            ]
                        ]
                    ]
                    Bulma.navbarMenu [
                        prop.id "agn-navbar-menu"
                        prop.children [ 
                            Bulma.navbarEnd.div [
                                githubLink
                                userView user
                            ]
                        ] 
                    ]
                ] 
            ]
        ]
    ]
    
let handler env : HttpHandler =
    fun next ctx -> 
        let user = ctx.GetUser() |> Result.toOption
        ctx.RespondWithHtmlFragment(env, navbarView user)