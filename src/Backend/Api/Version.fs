module Backend.Api.Version

open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine
open Saturn
open Giraffe
open Backend.Extensions
open Backend

        
let versionView =
    Bulma.section [
        Html.p (sprintf "Version: %s" Version.version)
    ]
    
let getVersion env : HttpFunc =
    fun ctx -> ctx.RespondWithHtml(env, versionView)
    
let controller env = controller {
        index (getVersion env)
    }