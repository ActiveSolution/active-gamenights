module Backend.CommonHttpHandlers
open Giraffe
open Microsoft.AspNetCore.Http


let requireUsername : HttpHandler =
    fun next (ctx: HttpContext) ->
        match ctx.GetUser() with
        | Ok _ -> next ctx
        | Error _ -> redirectTo false "/user/add" next ctx

