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
    let _autoscroll = attr "autoscroll" "true"
            
    [<RequireQualifiedAccess>]
    module TurboStream =
        let private template = tag "template"
        let render (turboStreams: seq<TurboStream>) =
            [
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
            
        let update targetId content =
            TurboStream.Create("update", targetId, Some content)

        let remove targetId =
            TurboStream.Create("remove", targetId, None)
            
        let writeTurboStreamContent statusCode (ts: seq<TurboStream>) (ctx: HttpContext) =
            ctx.SetContentType "text/vnd.turbo-stream.html"
            ctx.SetStatusCode statusCode
            
            render ts
            |> RenderView.AsString.htmlNodes
            |> ctx.WriteStringAsync 
        
    module Stimulus =
        type Action =
            { DomEvent: string
              Controller: string
              Action: string }
        type Target =
            { Controller: string
              TargetName: string }
        type Value =
            { Controller: string
              ValueName: string
              Value: string }
        type CssClass =
            { Controller: string
              ClassName: string
              ClassValue: string }
        let controller name = attr "data-controller" name
        let controllers names = attr "data-controller" (names |> String.concat " ")
        let action (action: Action) =
            attr "data-action" (sprintf "%s->%s#%s" action.DomEvent action.Controller action.Action) 
        let actions (actions: seq<Action>) =
            actions
            |> Seq.map (fun a -> sprintf "%s->%s#%s" a.DomEvent a.Controller a.Action)
            |> String.concat " "
            |> attr "data-action"
            
        let target (target: Target) =
            attr (sprintf "data-%s-target" target.Controller) target.TargetName
            
        let value (value: Value) =
            attr (sprintf "data-%s-%s-value" value.Controller value.ValueName) value.Value
        
        let cssClass (klass: CssClass) =
            attr (sprintf "data-%s-%s-class" klass.Controller klass.ClassName) klass.ClassValue
        
        module Controllers =
            let loadingButton =
                let ctrl = "css-class"
                [ controller ctrl
                  cssClass { Controller = ctrl; ClassName = "name"; ClassValue = "is-loading" }
                  action { DomEvent = "click"; Controller = ctrl; Action = "addClass" } ]
