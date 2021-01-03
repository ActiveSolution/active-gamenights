module Backend.Api.ConfirmedGameNight

open Giraffe
open FSharpPlus.Data
open Saturn
open FsToolkit.ErrorHandling
open Backend
open Feliz.Bulma.ViewEngine
open Domain
open Feliz.ViewEngine
open Turbo
open Backend.Api.Shared
    
    
let confirmedGameNightCard currentUser (gn: ConfirmedGameNight) =
    Html.turboFrame [
        prop.id ("confirmed-game-night-" + gn.Id.AsString)
        prop.children [
            Bulma.card [
                prop.classes [ "mb-5" ]
                prop.children [
                    Bulma.cardHeader [
                        Bulma.cardHeaderTitle.p (gn.CreatedBy.Val + " wants to play")
                    ]
                    Bulma.cardContent [
                        for gameName, votes in gn.GameVotes |> NonEmptyMap.toList do
                            Html.unorderedList [
                                Html.listItem [
                                    GameNight.gameCard gn.Id gameName votes currentUser
                                ] 
                            ] 
                        GameNight.dateCard gn.Id gn.Date (gn.Players |> NonEmptySet.toSet) currentUser
                    ]
                ]
            ]
        ]
    ]
    
let gameNightsView currentUser confirmed =
    Html.turboFrame [
        prop.id "confirmed-game-nights"
        prop.children [
            Bulma.container [
                Bulma.title.h2 "Confirmed game nights"
                Bulma.section [
                    for gameNight in confirmed do confirmedGameNightCard currentUser gameNight
                ]
            ]
        ]
    ]

let toMissingUserError (ValidationError err) = ApiError.MissingUser err

let getAll env : HttpFunc =
    fun ctx -> 
        taskResult {
            let! confirmed = Storage.getAllConfirmedGameNights env
            let! currentUser = ctx.GetUser() |> Result.mapError toMissingUserError
            return gameNightsView currentUser confirmed 
        } 
        |> (fun view -> ctx.RespondWithHtml(env, view))
        
let controller env = controller {
    
    index (getAll env)
}
