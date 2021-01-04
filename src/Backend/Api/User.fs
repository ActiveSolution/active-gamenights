module Backend.Api.User

open Backend
open FsToolkit.ErrorHandling.Operator.Result
open Giraffe
open Saturn
open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling
open Domain
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine
open Backend.Api.Shared

let addUserView (user: User option) =
    Html.form [
        prop.action "/user"
        prop.method "POST"
        prop.children [
            Bulma.title.h1 "Who are you?"
            Bulma.fieldControl [
                Html.input [
                    prop.type'.text
                    prop.classes [ "input" ]
                    prop.name HttpContext.usernameKey
                    prop.autoFocus true
                    prop.value (user |> Option.map (fun u -> u.Val) |> Option.defaultValue "")
                ]
            ] 
            Bulma.button.button [
                color.isPrimary
                prop.type'.submit
                prop.name "submit"
                prop.text "OK"
            ]   
        ]
    ]

let addUser env : HttpFunc =
    fun ctx ->
        let user = ctx.GetUser() |> Result.toOption
        ctx.RespondWithHtml(env, (addUserView user))
    
let createUser : HttpFunc =
    fun ctx ->
        result {
            let! username =
                ctx.GetFormValue HttpContext.usernameKey
                |> Result.requireSome (sprintf "missing form value %s" HttpContext.usernameKey |> ValidationError |> ApiError.Validation)
            let! user =
                username
                |> User.create 
                |> Result.mapError ApiError.Validation
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
