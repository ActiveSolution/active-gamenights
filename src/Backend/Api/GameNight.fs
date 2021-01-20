module Backend.Api.GameNight

open Giraffe.ViewEngine
open FsHotWire.Giraffe
open Giraffe
open Saturn
open Backend.Extensions
open Backend


    
let gameNightsView =
    [
        turboFrame [ _id "confirmed-game-nights"; _src "/confirmedgamenight" ] []
        turboFrame [ _id "proposed-game-nights"; _src "/proposedgamenight" ] []
    ]

let getAll env : HttpFunc =
    fun ctx ->
        ctx.RespondWithHtml(env, gameNightsView)
    
let controller env = controller {
    plug [ All ] CommonHttpHandlers.requireUsername
    
    index (getAll env)
}

