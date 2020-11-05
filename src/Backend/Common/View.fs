module Backend.Common.View

open Backend
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine


module Helpers =
    let fieldControl (elements : ReactElement list) =
        Bulma.field.div [
            Bulma.control.div elements 
        ]
        
    let fieldLabelControl (label: string) (elements : ReactElement list) =
        Bulma.field.div [
            Bulma.label label
            Bulma.control.div elements 
        ]
        
    let faIcon classes =
        Bulma.icon [
            prop.children [
                Html.i [
                    prop.classes classes 
                ]
            ]
        ]
        
    let plusIcon = 
        faIcon [ "fas"; "fa-plus" ]
        
let githubLink =
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
    
let navbar user =
    Bulma.navbar [
        prop.id "agn-navbar"
        prop.custom ("data-turbolinks-permanent", "")
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
                        match user with
                        | Some (User username) ->
                            Bulma.navbarItem.a [
                                prop.id "logout-button"
                                prop.custom ("data-username", username)
                                prop.href "#"
                                prop.children [
                                    Html.text ("logout " + username)
                                ]
                            ]
                        | None _ -> 
                            Html.none
                    ]
                ] 
            ]
        ] 
    ]
    
let html (BasePath basePath) (user: User option) (content : ReactElement) =
    Html.html [
        Html.head [
            Html.meta [ prop.charset.utf8 ]
            Html.meta [ 
                prop.name "viewport"
                prop.content "width=device-width, initial-scale=1"
            ]
            Html.title "Active Game Night"
            Html.base' [
                prop.href basePath
            ]
            Html.link [
                prop.rel "apple-touch-icon"
                prop.sizes "180x180"
                prop.href "Icons/apple-touch-icon.png"
            ]
            Html.link [
                prop.rel "icon" 
                prop.type' "image/png" 
                prop.sizes "32x32" 
                prop.href "Icons/favicon-32x32.png"
            ]
            Html.link [
                prop.rel "icon"
                prop.type' "image/png"
                prop.sizes "16x16"
                prop.href "Icons/favicon-16x16.png" 
            ]
            Html.link [
                prop.rel "shortcut icon"
                prop.href "/Icons/favicon.ico"
                prop.custom ("type", "image/x-icon")
            ]
            Html.link [ 
                prop.rel "manifest" 
                prop.href "Icons/site.json"
            ]
            Html.link [
                prop.rel.stylesheet
                prop.href "https://cdn.jsdelivr.net/npm/bulma@0.9.0/css/bulma.min.css"
            ]
            Html.script [
                prop.defer true
                prop.src "https://use.fontawesome.com/releases/v5.14.0/js/all.js"
            ]
            Html.script [ prop.src "https://cdnjs.cloudflare.com/ajax/libs/turbolinks/5.2.0/turbolinks.js" ]
            Html.script [
                prop.src "/js/App.js"
            ]
            Html.link [
                prop.rel.stylesheet
                prop.href "/Styles/main.css"
            ]
        ]
        Html.body [
            navbar user
            Bulma.section [
                Bulma.container [
                    content
                ]
            ]
        ]
    ] |> Render.htmlDocument
