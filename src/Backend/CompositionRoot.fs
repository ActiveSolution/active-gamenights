module Backend.CompositionRoot

open Backend
open Giraffe

let config = Configuration.config.Value

let private storage = Storage.Service config.ConnectionString

module Browser =
    let userController : HttpHandler = Browser.User.Controller.controller config.BasePath config.Domain
    let gameNightController : HttpHandler = Browser.GameNight.Controller.controller storage config.BasePath config.Domain

module Api =
    let gameNightController : HttpHandler = Api.GameNight.controller storage
    
