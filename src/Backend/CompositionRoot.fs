module Backend.CompositionRoot

open Backend
open Giraffe

let config = Configuration.config.Value

type BackendEnv() =
    interface Storage.IStorage with member _.Tables = Storage.live config.ConnectionString
let backendEnv = BackendEnv()

module Browser =
    let userController : HttpHandler = Browser.User.Controller.controller config.BasePath config.Domain
    let gameNightController : HttpHandler = Browser.GameNight.Controller.controller backendEnv config.BasePath config.Domain

module Api =
    let gameNightController : HttpHandler = Api.GameNight.controller backendEnv
    
