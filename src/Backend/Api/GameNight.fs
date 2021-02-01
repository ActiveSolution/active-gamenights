module Backend.Api.GameNight

open FsHotWire.Giraffe
open Giraffe
open Saturn
open Backend.Extensions
open Backend
open Backend.Api.Shared
open FsToolkit.ErrorHandling

    
module private Views =

    open Giraffe.ViewEngine

    let private spinner = 
            nav [ _class "level" ] [
            div [ _class "icon level-item has-text-centered" ] [
                i [ _class "fas fa-spinner fa-spin fa-3x" ] [ ]
            ]
        ]
    let gameNightsView currentUser confirmed proposed =
        [
            ConfirmedGameNight.Views.gameNightsView currentUser confirmed
            ProposedGameNight.Views.gameNightsView currentUser proposed
        ]

let private getGameNights env =
    async {
        let! confirmed = Storage.GameNights.getAllConfirmedGameNights env |> Async.StartChild
        let! proposed = Storage.GameNights.getAllProposedGameNights env |> Async.StartChild
        let! c = confirmed
        let! p = proposed
        return (c, p)
    }

let getAll env : HttpFunc =
    fun ctx ->
        taskResult {
            let! (confirmed, proposed) = getGameNights env
            let! currentUser = ctx.GetUser() |> Result.mapError ApiError.MissingUser
            return Views.gameNightsView currentUser confirmed proposed

        }
        |> (fun view -> ctx.RespondWithHtml(env, Page.GameNights, view))
    
let controller env = controller {
    plug [ All ] CommonHttpHandlers.requireUsername
    
    index (getAll env)
}

