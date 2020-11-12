module Backend.Startup

open System
open Feliz.ViewEngine
open Giraffe
open Microsoft.AspNetCore.Http
open Saturn
open Microsoft.Extensions.Logging
open Turbolinks
        
        
let endpointPipe =
    pipeline {
        plug putSecureBrowserHeaders
        plug fetchSession
        plug head
    }
    
let userRouter =
    router {
        forward "" CompositionRoot.Browser.userController
    }
    
let requireUsername : HttpHandler =
    fun next (ctx: HttpContext) ->
        match ctx.GetUser() with
        | Ok _ -> next ctx
        | Error _ -> redirectTo false "/user/add" next ctx

let about = ((CompositionRoot.config.BasePath, CompositionRoot.config.Domain) ||> Browser.Common.View.versionView |> Render.htmlView |> htmlString)

let browserRouter =
    router {
        pipe_through requireUsername
        pipe_through endpointPipe
        forward "" CompositionRoot.Browser.gameNightController
        forward "/gamenight" CompositionRoot.Browser.gameNightController
    }
    
let notFoundHandler : HttpHandler =
    fun next ctx ->
        (setStatusCode 404 >=> text "Not found") next ctx
        
let apiRouter =
    router {
        forward "/gamenight" CompositionRoot.Api.gameNightController
        not_found_handler notFoundHandler
    }

let topRouter =
    router {
        get "/about" about
        get "/version" about
        forward "/user" userRouter
        forward "/api" apiRouter
        forward "" browserRouter
    }
        
let webApp =
    choose [ topRouter
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
