module Backend.User.Controller

open Backend
open Giraffe
open Saturn
open Microsoft.AspNetCore.Http
open Backend.Extensions
open Backend.Turbolinks
open FsToolkit.ErrorHandling

let addUser (ctx: HttpContext) =
    let basePath = CompositionRoot.config.BasePath
    let domain = CompositionRoot.config.Domain
    Views.addUserView
    |> Common.View.html basePath domain None
    |> Controller.html ctx
    
let createUser (ctx : HttpContext) =
    ctx.GetFormValue HttpContext.usernameKey
    |> Result.requireSome (sprintf "missing form value %s" HttpContext.usernameKey |> ValidationError)
    |> Result.map (Helpers.replaceWhiteSpace)
    |> Result.bind (Domain.createUser)
    |> function
        | Ok (User username) ->
            ctx.SetUsername username
            Turbolinks.redirect ctx "/"
        | Error err -> Response.badRequest ctx err
    
let clearUser (ctx : HttpContext) (_ : string) =
    ctx.ClearUsername()
    Turbolinks.redirect ctx "/user/add"
        
let controller = controller {
    add addUser
    create createUser
    delete clearUser
}
