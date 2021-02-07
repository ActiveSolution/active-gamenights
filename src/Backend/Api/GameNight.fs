module Backend.Api.GameNight

open FsHotWire.Giraffe
open Giraffe
open Saturn
open Backend.Extensions
open Backend
open Backend.Api.Shared
open FsToolkit.ErrorHandling
open Domain

    
module private Views =

    open Giraffe.ViewEngine

    let private spinner = 
            nav [ _class "level" ] [
            div [ _class "icon level-item has-text-centered" ] [
                i [ _class "fas fa-spinner fa-spin fa-3x" ] [ ]
            ]
        ]
    let gameNightsView allGames currentUser confirmed proposed =
        [ ConfirmedGameNight.Views.gameNightsView allGames currentUser confirmed
          ProposedGameNight.Views.gameNightsView allGames currentUser proposed ]


let getAll env : HttpFunc =
    let getData env =
        async {
            let! confirmed = Storage.GameNights.getAllConfirmedGameNights env |> Async.StartChild
            let! proposed = Storage.GameNights.getAllProposedGameNights env |> Async.StartChild
            let! allGames = Storage.Games.getAllGames env |> Async.map Game.toMap |> Async.StartChild
            let! c = confirmed
            let! p = proposed
            let! gs = allGames
            return (c, p, gs)
        }
    fun ctx ->
        taskResult {
            let! (confirmed, proposed, allGames) = getData env
            let! currentUser = ctx.GetUser() |> Result.mapError ApiError.MissingUser
            return Views.gameNightsView allGames currentUser confirmed proposed
        }
        |> (fun view -> ctx.RespondWithHtml(env, Page.GameNights, view))
    
let controller env = controller {
    plug [ All ] CommonHttpHandlers.requireUsername
    
    index (getAll env)
}

