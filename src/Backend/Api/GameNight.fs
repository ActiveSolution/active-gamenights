module Backend.Api.GameNight

open Feliz.ViewEngine
open Giraffe
open Saturn
open Backend.Extensions
open Backend
open Feliz.Bulma.ViewEngine
open FsHotWire.Feliz

    
let gameNightsView =
    Bulma.container [
        Html.turboFrame [
            prop.id "confirmed-game-nights"
            prop.src "/confirmedgamenight"
        ]
        Html.turboFrame [
            prop.id "proposed-game-nights"
            prop.src "/proposedgamenight"
        ]
    ]

let getAll env : HttpFunc =
    fun ctx ->
        ctx.RespondWithHtml(env, gameNightsView)
    
let controller env = controller {
    plug [ All ] CommonHttpHandlers.requireUsername
    
    index (getAll env)
}

