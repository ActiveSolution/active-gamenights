module FsHotWire

open Feliz.ViewEngine
open Giraffe
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

let (|AcceptTurboStream|_|) (req: HttpRequest) =
    if req.Headers.["Accept"].ToString().Contains("turbo-stream") then
        Some AcceptTurboStream
    else
        None
    
[<RequireQualifiedAccess>]
module Turbo =
    let redirect (uri: string) : HttpFunc =
        fun ctx ->
            task {
                ctx.SetStatusCode 303
                ctx.SetHttpHeader "Location" uri
                return Some ctx
            }
            
module Feliz =
    type TurboStream =
        private 
            { Action: string
              Content: ReactElement
              Target: string }
        with 
            static member internal Create(action, target, content) =
                { Target = target
                  Action = action
                  Content = content }

    
    type prop with
        static member targetTurboFrame (id: string) =
            prop.custom ("data-turbo-frame", id)
        static member disableTurboDrive =
            prop.custom("data-turbo", false)
            
    module prop =
        [<Erase>]
        type targetTurboFrame =
            static member inline top = Interop.mkAttr "data-turbo-frame" "_top"

    type Html with 
        static member inline turboFrame xs = Interop.createElement "turbo-frame" xs
        static member inline turboFrame (children: #seq<ReactElement>) = Interop.reactElementWithChildren "turbo-frame" children
        static member inline turboStream xs = Interop.createElement "turbo-stream" xs
        static member inline turboStream (children: #seq<ReactElement>) = Interop.reactElementWithChildren "turbo-stream" children
        
    [<RequireQualifiedAccess>]
    module TurboStream =
        let private render (turboStreams: TurboStream list) =
            Html.body [
                for ts in turboStreams do
                    Html.turboStream [
                        prop.action ts.Action
                        prop.target ts.Target
                        prop.children [
                            Html.template [
                                ts.Content
                            ]
                        ]
                    ]
                ]
            
        let append targetId content =
            TurboStream.Create("append", targetId, content)
            
        let replace targetId content =
            TurboStream.Create("replace", targetId, content)
            
        let writeTurboStreamContent statusCode (ts: TurboStream list) (ctx: HttpContext) =
            ctx.SetContentType "text/html; turbo-stream"
            ctx.SetStatusCode statusCode
            
            render ts
            |> Render.htmlView
            |> ctx.WriteStringAsync 
        
