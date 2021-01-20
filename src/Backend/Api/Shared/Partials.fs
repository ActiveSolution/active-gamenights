namespace Backend.Api.Shared

        
module Partials =
        
    open Giraffe.ViewEngine

    let submitButton (text: string) =
        div [ 
            _class "field" 
            _style "margin-top: 10px;"
        ] [ 
            div [ _class "control" ] [
                button [ _class "button is-primary"; _type "submit" ] [
                    str text
                ]
            ]
        ]

    let submitButtonWithCancel (okText: string) (cancelText: string) cancelHref =
        div [ 
            _class "field is-grouped" 
            _style "margin-top: 10px;"
        ] [
            div [ _class "control"] [
                button [
                    _class "button is-primary"
                    _type "submit"
                ] [ str okText]
            ]
            div [ _class "control"] [
                a [
                    _class "button is-light"
                    _href cancelHref
                ] [ str cancelText]
            ]
        ]

module Icons =
    open Giraffe.ViewEngine
    let plusIcon = 
        span [ _class "icon" ] [ 
            i [ _class "fas fa-plus" ] [ ] 
        ]