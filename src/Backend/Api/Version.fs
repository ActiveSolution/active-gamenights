module Backend.Api.Version

open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine
open Giraffe
open Backend.Extensions
open Backend


let versionView =
    Bulma.section [
        Html.p (sprintf "Version: %s" Version.version)
    ]
    
let handler env : HttpHandler =
    fun _ ctx -> ctx.RespondWithHtml(env, versionView)
