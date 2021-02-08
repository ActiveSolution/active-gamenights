module Backend.Api.Shared.HtmlViews

open FsHotWire.Giraffe
open Backend
open Backend.Api
open Giraffe.ViewEngine

let htmlHead (settings: ITemplateSettings) =
    head [ _title "Active Game Night" ] [ 
        ``base`` [ _href settings.BasePath.Val ] 
        meta [ _charset "utf8" ]
        meta [ _name "viewport"; _content "width=device-width, initial-scale=1" ]
        link [
            _rel "apple-touch-icon"
            _sizes "180x180"
            _href "Icons/apple-touch-icon.png"
        ] 
        link [
            _rel "icon" 
            _type "image/png" 
            _sizes "32x32" 
            _href "Icons/favicon-32x32.png"
        ]
        link [
            _rel "icon"
            _type "image/png"
            _sizes "16x16"
            _href "Icons/favicon-16x16.png" 
        ]
        link [
            _rel "shortcut icon"
            _href "/Icons/favicon.ico"
            _type "image/x-icon"
        ]
        link [ 
            _rel "manifest" 
            _href "/Icons/site.json"
        ]
        link [
            _rel "stylesheet"
            _href "https://cdn.jsdelivr.net/npm/bulma@0.9.0/css/bulma.min.css"
        ]
        script [
            _defer
            _src "https://use.fontawesome.com/releases/v5.14.0/js/all.js"
        ] []
        script [
            _async 
            _defer
            attr "data-domain" settings.Domain.Val
            _src "https://plausible.io/js/plausible.js"
        ] []
        script [
            _async
            _defer
            _src "/Scripts/bundle.js"
            _type "text/javascript"
        ] []
        link [
            _rel "stylesheet"
            _href "/Styles/main.css"
        ]
    ]

let fragment content =
    content
    |> RenderView.AsString.htmlNode
        

let fullPage env user page content =
    html [] [
        htmlHead env
        body [
            Stimulus.controllers [ "unvoted-count"; "turbo-stream" ]
            Stimulus.action { DomEvent = "refresh-vote-count:connected"; Controller = "unvoted-count"; Action = "refresh" }
            Stimulus.value { Controller = "turbo-stream"; ValueName = "url"; Value = "http://localhost:8085/sse-notifications" }
            _class "has-navbar-fixed-top" 
        ] [
            Navbar.Views.navbarView user page
            turboFrame [ _id "content" ] [
                yield! content
            ]
        ]
    ] 
    |> RenderView.AsString.htmlDocument
