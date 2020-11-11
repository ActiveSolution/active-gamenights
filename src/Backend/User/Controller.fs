module Backend.User.Controller

open Backend
open Giraffe
open Saturn
open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling

let addUser basePath domain (ctx: HttpContext) =
    Views.addUserView
    |> Common.View.html basePath domain None
    |> Controller.html ctx
    
let createUser (ctx : HttpContext) =
    ctx.GetFormValue HttpContext.usernameKey
    |> Result.requireSome (sprintf "missing form value %s" HttpContext.usernameKey |> ValidationError |> AppError.Validation)
    |> Result.map (Helpers.replaceWhiteSpace)
    |> Result.bind (Domain.createUser >> Result.mapError AppError.Validation)
    |> Result.map (fun (User username) -> 
        ctx.SetUsername username
        Redirect "/")
    |> BrowserResult.handle ctx
    
let clearUser (ctx : HttpContext) (_ : string) =
    ctx.ClearUsername()
    Ok (Redirect "/user/add")
    |> BrowserResult.handle ctx
        
let controller basePath domain = controller {
    add (addUser basePath domain)
    create createUser
    delete clearUser
}
