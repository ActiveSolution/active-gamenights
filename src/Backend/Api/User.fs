module Backend.Api.User

open Backend
open FsToolkit.ErrorHandling.Operator.Result
open Giraffe
open Saturn
open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling
open Domain
open FSharp.UMX


module private Views =
    open Giraffe.ViewEngine
    let addUserView (user: string<CanonizedUsername> option) =
        section [ _class "section" ] [
            div [ _class "container" ] [
                form [
                    _action "/user"
                    _method "POST"
                ] [
                    h1 [ _class "title is-1" ] [ str "Who are you?" ]
                    div [ _class "field" ] [
                        div [ _class "control has-icons-left" ] [
                            input [
                                _type "text"
                                _class "input"
                                _name HttpContext.usernameKey
                                _autofocus
                                _value (user |> Option.map (fun u -> %(Username.toDisplayName u)) |> Option.defaultValue "")
                            ]
                            span [ _class "icon is-small is-left" ] [
                                i [ _class "fas fa-user" ] [ ]
                            ]
                        ]
                    ]
                    div [ _class "field" ] [
                        div [ _class "control" ] [
                            button [ _class "button is-primary"; _type "submit"; _name "submit" ] [ str "OK" ]
                        ]
                    ]
                ]
            ]
        ]

let addUser env : HttpFunc =
    fun ctx ->
        let user = ctx.GetUser() |> Result.toOption
        ctx.RespondWithHtml(env, (Views.addUserView user))
    
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
    
    add (addUser env)
    create createUser
    delete clearUser
}
