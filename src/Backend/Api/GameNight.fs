module Backend.Api.GameNight

open Giraffe
open Infrastructure
open Saturn
open Backend.Extensions
open Backend
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
    let gameNightsView allGames currentUser proposed =
        ProposedGameNight.Views.gameNightsView allGames currentUser proposed 


let getAll env : HttpFunc =
    let getData env =
        async {
            let! proposed = Storage.GameNights.getAllProposedGameNights env |> Async.StartChild
            let! allGames = Storage.Games.getAllGames env |> Async.map Game.toMap |> Async.StartChild
            let! p = proposed
            let! gs = allGames
            return (p, gs)
        }
    fun ctx ->
        taskResult {
            let refreshVoteCount = ctx.TryGetQueryStringValue "refreshVoteCount" |> Option.bind (bool.tryParse) |> Option.defaultValue false
            let! (proposed, allGames) = getData env
            let! currentUser = ctx.GetCurrentUser() |> Result.mapError ApiError.MissingUser
            return Views.gameNightsView refreshVoteCount allGames currentUser.Name proposed
        }
        |> (fun view -> ctx.RespondWithHtml(env, Page.GameNights, view))
    
let controller env = controller {
    plug [ All ] CommonHttpHandlers.requireUsername
    
    index (getAll env)
}

