module Backend.Startup

open System
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Saturn
open Microsoft.Extensions.Logging
        
let endpointPipe =
    pipeline {
        plug putSecureBrowserHeaders
        plug fetchSession
        plug head
    }
    
let rewriteHttpMethod : HttpHandler =
    fun next (ctx: HttpContext) ->
        match ctx.GetFormValue("_method") with
        | Some method when method = "delete" ->
            ctx.Request.Method <- "delete"
        | Some method when method = "put" ->
            ctx.Request.Method <- "put"
        | _ -> ()
        next ctx
        
        
let privateCachingWithQueries duration queryParams : HttpHandler =
    responseCaching
        (Private duration)
        (Some "Accept, Accept-Encoding")
        (Some queryParams)
        
let fragments = router {
    get "/proposedgamenight/addgame" (privateCachingWithQueries (TimeSpan.FromHours 24.) [| "index" |] >=> CompositionRoot.Api.Fragments.addGameInput)
    get "/proposedgamenight/adddate" (privateCachingWithQueries (TimeSpan.FromHours 24.) [| "index" |] >=> CompositionRoot.Api.Fragments.addDateInput)
}

let browserRouter =
    router {
        pipe_through rewriteHttpMethod
        pipe_through endpointPipe
        forward "" CompositionRoot.Api.gameNightController
        forward "/user" CompositionRoot.Api.userController
        forward "/fragments" fragments
        forward "/confirmedgamenight" CompositionRoot.Api.confirmedGameNightController
        forward "/proposedgamenight" CompositionRoot.Api.proposedGameNightController
        forward "/gamenight" CompositionRoot.Api.gameNightController
        get "/navbar" CompositionRoot.Api.navbarPage
        get "/version" CompositionRoot.Api.versionPage
        get "/about" CompositionRoot.Api.versionPage
    }
    
let notFoundHandler : HttpHandler =
    setStatusCode 404 >=> text "Not found"
        
let webApp =
    choose [ browserRouter
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
let configureServices (s: IServiceCollection) =
    s.AddResponseCaching() |> ignore
    s
let configureApp (a: IApplicationBuilder) =
    a.UseResponseCaching() |> ignore
    a
    
application {
    url ("http://*:" + CompositionRoot.config.ServerPort.ToString() + "/")
    error_handler errorHandler
    pipe_through endpointPipe
    use_router webApp
    use_gzip
    service_config configureServices
    app_config configureApp
    logging configureLogging
    memory_cache
    use_static CompositionRoot.config.PublicPath
}
|> run
