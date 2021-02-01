module Backend.Api.Version

open Giraffe.ViewEngine
open Giraffe
open Backend.Extensions
open Backend


let versionView =
    section [ _class "section"  ] [
        p [] [ str (sprintf "Version: %s" Version.version) ]
    ]
    
let handler env : HttpHandler =
    fun _ ctx -> ctx.RespondWithHtml(env, Page.Version, versionView)
