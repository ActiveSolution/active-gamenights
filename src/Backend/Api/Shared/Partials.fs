[<RequireQualifiedAccess>]
module Backend.Api.Shared.Partials

open Backend.Turbo
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine
open Backend

module HtmlPage =
    let navbar =
        Html.turboFrame [
            prop.id "navbar"
            prop.src "/navbar"
        ]
        
    let htmlHead (settings: ITemplateSettings) =
        Html.head [
            Html.meta [ prop.charset.utf8 ]
            Html.meta [ 
                prop.name "viewport"
                prop.content "width=device-width, initial-scale=1"
            ]
            Html.title "Active Game Night"
            Html.base' [
                prop.href settings.BasePath.Val
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
            Html.script [
                prop.async true
                prop.defer true
                prop.custom ("data-domain", settings.Domain.Val)
                prop.src "https://plausible.io/js/plausible.js"
            ]
            Html.script [
                prop.custom ("type", "module")
                prop.src "https://cdn.skypack.dev/pin/@hotwired/turbo@v7.0.0-beta.1-tPCW1AHTtVGOe5r89LWv/min/@hotwired/turbo.js"
            ]
            Html.link [
                prop.rel.stylesheet
                prop.href "/Styles/main.css"
            ]
        ]
        
    let fragment env (content: ReactElement) =
        Html.html [
            htmlHead env
            Html.body [
                Bulma.section [
                    Bulma.container [
                        content
                    ]
                ]
            ]
        ] |> Render.htmlDocument

    let fullPage env (content : ReactElement) =
        Html.html [
            htmlHead env
            Html.body [
                navbar 
                Bulma.section [
                    Bulma.container [
                        content
                    ]
                ]
            ]
        ] |> Render.htmlDocument

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
