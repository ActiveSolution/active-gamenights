module Backend.CompositionRoot

open Backend
open Giraffe

let config = Configuration.config.Value

type BackendEnv() =
    interface Storage.IStorage with member _.Tables = Storage.live config.ConnectionString
    interface IBrowser with 
        member _.Settings = 
            { new IBrowserSettings with 
                member _.BasePath = config.BasePath
                member _.Domain = config.Domain }
let env = BackendEnv()

module Browser =
    let userController : HttpHandler = Browser.User.Controller.controller env
    let gameNightController : HttpHandler = Browser.GameNight.Controller.controller env

module Api =
    let gameNightController : HttpHandler = Api.GameNight.controller env
    
