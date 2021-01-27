module FsHotWire

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

module Giraffe =
    open Giraffe.ViewEngine
    let turboFrame = tag "turbo-frame"
    let turboStream = tag "turbo-stream"
            
    type TurboStream =
        private 
            { Action: string
              Content: XmlNode option
              Target: string }
        with 
            static member internal Create(action, target, content) =
                { Target = target
                  Action = action
                  Content = content }
            member this.TargetId = this.Target

    
    let _targetTurboFrame = attr "data-turbo-frame"
    let _disableTurboDrive = attr "data-turbo" "false"
            
    [<RequireQualifiedAccess>]
    module TurboStream =
        let private template = tag "template"
        let private render (turboStreams: seq<TurboStream>) =
            body [] [
                for ts in turboStreams do
                    turboStream [
                        _action ts.Action
                        _target ts.Target 
                        ] [
                            template [] [
                                match ts.Content with
                                | Some c -> c
                                | None -> ()
                            ]
                        ]
                    ]
            
        let append targetId content =
            TurboStream.Create("append", targetId, Some content)
            
        let replace targetId content =
            TurboStream.Create("replace", targetId, Some content)

        let remove targetId =
            TurboStream.Create("remove", targetId, None)
            
        let writeTurboStreamContent statusCode (ts: seq<TurboStream>) (ctx: HttpContext) =
            ctx.SetContentType "text/html; turbo-stream"
            ctx.SetStatusCode statusCode
            
            render ts
            |> RenderView.AsString.htmlDocument
            |> ctx.WriteStringAsync 
        
