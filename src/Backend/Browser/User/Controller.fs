module Backend.Browser.User.Controller

open Backend
open Backend.Browser
open Giraffe
open Saturn
open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling
open Domain

let addUser env (ctx: HttpContext) =
    Views.addUserView
    |> Browser.Common.View.html env None
    |> Controller.html ctx
    
let createUser (ctx : HttpContext) =
    ctx.GetFormValue HttpContext.usernameKey
    |> Result.requireSome (sprintf "missing form value %s" HttpContext.usernameKey |> ValidationError |> BrowserError.Validation)
    |> Result.bind (User.create >> Result.mapError BrowserError.Validation)
    |> Result.map (fun (user) -> 
        ctx.SetUsername user.Val
        Redirect "/")
    |> BrowserResult.handle ctx
    
let clearUser (ctx : HttpContext) (_ : string) =
    ctx.ClearUsername()
    Ok (Redirect "/user/add")
    |> BrowserResult.handle ctx
        
let controller env = controller {
    add (addUser env)
    create createUser
    delete clearUser
}
