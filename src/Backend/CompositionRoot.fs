module Backend.CompositionRoot

open Backend
open Giraffe

let config = Configuration.config.Value

let private storage = Storage.Service config.ConnectionString

let userController : HttpHandler = User.Controller.controller config.BasePath config.Domain
let gameNightController : HttpHandler = GameNight.Controller.controller storage config.BasePath config.Domain

module Api =
    let gameNightController : HttpHandler = Api.GameNight.controller storage
    
