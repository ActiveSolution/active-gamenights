module Backend.Startup

open System
open Feliz.ViewEngine
open Giraffe
open Microsoft.AspNetCore.Http
open Saturn
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Logging
open Backend.Extensions
open Turbolinks
        
        
let endpointPipe =
    pipeline {
        plug putSecureBrowserHeaders
        plug fetchSession
        plug head
    }
    
let userRouter =
    router {
        forward "/user" (User.Controller.controller)
    }
    
let requireUsername : HttpHandler =
    fun next (ctx: HttpContext) ->
        match ctx.GetUser() with
        | Ok _ -> next ctx
        | Error _ -> redirectTo false "/user/add" next ctx

let aboutRouter =
    let about = ((CompositionRoot.config.BasePath, CompositionRoot.config.Domain) ||> Common.View.versionView |> Render.htmlView |> htmlString)
    router {
        get "/about" about
        get "/version" about
    }

let browserRouter =
    router {
        pipe_through requireUsername
        forward "" (GameNight.Controller.controller)
        forward "/gamenight" (GameNight.Controller.controller)
    }

let notFoundHandler : HttpHandler =
    fun next ctx ->
        task {
            return! (setStatusCode 404
                     >=> htmlString "<div>Not Found!</div>")
                        next
                        ctx
        }
        
let webApp =
    choose [ userRouter
             aboutRouter
             browserRouter
             notFoundHandler ]

let errorHandler: ErrorHandler =
    fun exn logger _next ctx ->
        match exn with
        | :? ArgumentException as a -> Response.badRequest ctx a.Message
        | _ ->
            let msg =
                sprintf "Exception for %s%s" ctx.Request.Path.Value ctx.Request.QueryString.Value

            logger.LogError(exn, msg)
            Response.internalError ctx ()
            
            
let configureLogging (log:ILoggingBuilder) =
    log.ClearProviders() |> ignore
    log.AddConsole() |> ignore
    
application {
    url ("https://*:" + CompositionRoot.config.ServerPort.ToString() + "/")
    error_handler errorHandler
    pipe_through endpointPipe
    use_router webApp
    use_gzip
    logging configureLogging
    memory_cache
    use_static "public"
    use_turbolinks
}
|> run
