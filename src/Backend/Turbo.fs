module Backend.Turbo

open Feliz.ViewEngine
open Giraffe
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

let (|AcceptTurboStream|_|) (req: HttpRequest) =
    if req.Headers.["Accept"].ToString().Contains("turbo-stream") then
        Some AcceptTurboStream
    else
        None
    
module Turbo =
    let redirect (uri: string) : HttpFunc =
        fun ctx ->
            task {
                ctx.SetStatusCode 303
                ctx.SetHttpHeader "Location" uri
                return Some ctx
            }

module prop =
    let targetTurboFrame (id: string) =
        prop.custom ("data-turbo-frame", id)

type Html with 
    static member inline turboFrame xs = Interop.createElement "turbo-frame" xs
    static member inline turboFrame (children: #seq<ReactElement>) = Interop.reactElementWithChildren "turbo-frame" children
    
    static member inline turboStream xs = Interop.createElement "turbo-stream" xs
    static member inline turboStream (children: #seq<ReactElement>) = Interop.reactElementWithChildren "turbo-stream" children
        
type TurboStream =
    | Append of ReactElement
    | Replace of ReactElement
module TurboStream =
    let private getContent ts =
        match ts with
        | Append c
        | Replace c -> c
        
    let private render action id (content: ReactElement list) =
        Html.turboStream [
            prop.action action
            prop.target id
            prop.children (Html.template content)
        ]
        
    let append id (content: ReactElement list) =
        render "append" id content
        |> Append
        
    let replace id (content: ReactElement list) =
        render "replace" id content
        |> Replace
        
    let writeTurboStreamContent (ts: TurboStream) (ctx: HttpContext) =
        ctx.SetContentType "text/html; turbo-stream"
        
        ts
        |> getContent
        |> Render.htmlView
        |> ctx.WriteStringAsync 
        
