module Backend.Api.ProposedGameNight

open Giraffe
open FSharpPlus.Data
open Microsoft.AspNetCore.Http
open Saturn
open FsToolkit.ErrorHandling
open Backend
open Feliz.Bulma.ViewEngine
open Domain
open Feliz.ViewEngine
open Turbo
open Backend.Api.Shared

    
let proposedGameNightCard currentUser (gn: ProposedGameNight) =
    Html.turboFrame [
        prop.id ("proposed-game-night-" + gn.Id.AsString)
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
                        for date, votes in gn.DateVotes |> NonEmptyMap.toList do
                            Html.unorderedList [
                                Html.listItem [
                                    GameNight.dateCard gn.Id date votes currentUser
                                ] 
                            ]
                    ]
                ]
            ]
        ]
    ]
    

let addProposedGameLink =
    Html.turboFrame [
        prop.id "add-proposed-game-night"
        prop.children [
            Html.a [
                prop.href "/proposedgamenight/add"
                prop.children [ Partials.plusIcon; Html.text "Add new game night" ]
            ]
        ]
    ]
    
let gameNightsView currentUser proposed =
    Html.turboFrame [
        prop.id "proposed-game-nights"
        prop.children [
            Bulma.container [
                Bulma.title.h2 "Proposed game nights"
                Bulma.section [
                    prop.children [ for gameNight in proposed do proposedGameNightCard currentUser gameNight ]
                ]
                addProposedGameLink
            ]
        ]
    ]
    
let addProposedGameNightView =
    Bulma.section [
        Bulma.title.h2 "Add proposed game night"
        Html.turboFrame [
            prop.id "add-proposed-game-night"
            prop.children [
                Html.form [
                    prop.targetTurboFrame "proposed-game-nights"
                    prop.method "POST"
                    prop.action "/proposedgamenight"
                    prop.children [
                        Partials.fieldLabelControl "What do you want to play?" [
                            Html.input [
                                prop.type'.text
                                prop.classes [ "input" ]
                                prop.name "Games"
                                prop.placeholder "Enter a game"
                            ]
                        ]
                        Partials.fieldLabelControl "When?" [
                            Html.input [
                                prop.type'.text
                                prop.classes [ "input" ]
                                prop.name "Dates"
                                prop.placeholder "Pick a date"
                            ]
                        ]
                        Partials.fieldControl [
                            Bulma.button.button [
                                color.isPrimary
                                prop.type'.submit
                                prop.text "Save"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
    
    
let addProposedGameNight env : HttpFunc =
    fun ctx ->
        ctx.RespondWithHtml(env, addProposedGameNightView)

[<CLIMutable>]
type CreateProposedGameNightForm =
    { Games : string list
      Dates : string list }

let toMissingUserError (ValidationError err) = ApiError.MissingUser err

let saveProposedGameNight env (ctx: HttpContext) : HttpFuncResult =
    taskResult {
        let! dto = ctx.BindFormAsync<CreateProposedGameNightForm>()
        let! user = ctx.GetUser() |> Result.mapError toMissingUserError
        
        let! req = Workflows.GameNights.ProposeGameNightRequest.create (dto.Games, dto.Dates, user) |> Result.mapError ApiError.Validation
        let gn = Workflows.GameNights.proposeGameNight req
        
        let! _ = Storage.saveProposedGameNight env gn
        return "/proposedgamenight"
    }
    |> ctx.RespondWithRedirect
    
let getAll env : HttpFunc =
    fun ctx -> 
        taskResult {
            let! proposed = Storage.getAllProposedGameNights env
            printfn "fetched %i proposed game nights" (proposed |> List.length)
            let! currentUser = ctx.GetUser() |> Result.mapError toMissingUserError
            return gameNightsView currentUser proposed 
        } 
        |> (fun view -> ctx.RespondWithHtml(env, view))
        
let controller env = controller {
    plug [ All ] CommonHttpHandlers.requireUsername
    
    index (getAll env)
    add (addProposedGameNight env)
    create (saveProposedGameNight env)

}
