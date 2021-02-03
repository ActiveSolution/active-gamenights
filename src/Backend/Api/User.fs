module Backend.Api.User

open Backend
open FsToolkit.ErrorHandling.Operator.Result
open Giraffe
open Saturn
open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling
open Domain


module private Views =
    open Giraffe.ViewEngine
    open FsHotWire.Giraffe

    let addUserView =
        section [ _class "section" ] [
            div [ _class "container" ] [
                form [
                    _action "/user"
                    _method "POST"
                ] [
                    h1 [ _class "title is-1" ] [ str "What's your name?" ]
                    div [ _class "field" ] [
                        div [ _class "control has-icons-left" ] [
                            input [
                                _type "text"
                                _class "input"
                                _name HttpContext.usernameKey
                                _autofocus
                            ]
                            span [ _class "icon is-small is-left" ] [
                                i [ _class "fas fa-user" ] [ ]
                            ]
                        ]
                    ]
                    div [ _class "field" ] [
                        div [ _class "control" ] [
                            button [ 
                                yield! Stimulus.Controllers.loadingButton
                                _class "button is-primary"; _type "submit"; _name "submit" 
                            ] [ str "Ok" ]
                        ]
                    ]
                ]
            ]
        ]

let addUser env : HttpFunc =
    fun ctx ->
        ctx.RespondWithHtml(env, Page.User, Views.addUserView)
    
let createUser : HttpFunc =
    fun ctx ->
        result {
            let! username =
                ctx.GetFormValue HttpContext.usernameKey
                |> Result.requireSome (sprintf "missing form value %s" HttpContext.usernameKey |> ApiError.BadRequest)
            let! user =
                username
                |> Username.create 
                |> Result.mapError ApiError.BadRequest
            ctx.SetUsername user
            return ctx.TryGetQueryStringValue "redirect" |> Option.defaultValue "/"
        }
        |> ctx.RespondWithRedirect
    
let clearUser (ctx : HttpContext) (_ : string) =
    ctx.ClearUsername()
    ctx.RespondWithRedirect "/user/add"
        
let controller env = controller {
    plug [ Delete ] CommonHttpHandlers.requireUsername
    plug [ Add ] (CommonHttpHandlers.privateCaching (System.TimeSpan.FromHours 1.))
    
    add (addUser env)
    create createUser
    delete clearUser
}
