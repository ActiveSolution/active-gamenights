module Backend.User.Views

open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine
open Backend.Common.View.Helpers
open Backend.Extensions

let addUserView =
    Html.form [
        prop.action "/user"
        prop.method "POST"
        prop.children [
            Bulma.title.h1 "Who are you?"
            fieldControl [
                Html.input [
                    prop.type'.text
                    prop.classes [ "input" ]
                    prop.name HttpContext.usernameKey
                    prop.autoFocus true
                ]
            ] 
            Bulma.button.button [
                color.isPrimary
                prop.type'.submit
                prop.text "OK"
            ]   
        ]
    ]
