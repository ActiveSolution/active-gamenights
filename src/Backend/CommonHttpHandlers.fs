module Backend.CommonHttpHandlers
open System
open System.Security.Claims
open Giraffe
open Microsoft.AspNetCore.Http
open Domain


let requireUsername : HttpHandler =
    fun next (ctx: HttpContext) ->
        match ctx.Session.GetString(HttpContext.userKey) |> User.deserialize with
        | Ok user ->
            let claims = 
                [ Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                  Claim(ClaimTypes.Name, user.Name.ToString()) ]
            let principal = ClaimsPrincipal(ClaimsIdentity(claims, "Anonymous"))
            ctx.User <- principal
            next ctx
        | Error _ -> 
            redirectTo false "/user/add" next ctx

let privateCachingWithQueries duration queryParams : HttpHandler =
    responseCaching
        (Private duration)
        (Some "Accept, Accept-Encoding")
        (Some queryParams)
        

let privateCaching duration : HttpHandler =
    responseCaching
        (Private duration)
        (Some "Accept, Accept-Encoding")
        (None)