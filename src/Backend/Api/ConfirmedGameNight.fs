module Backend.Api.ConfirmedGameNight

open Giraffe
open FSharpPlus.Data
open Saturn
open FsToolkit.ErrorHandling
open Backend
open Domain
open Backend.Api.Shared
open FSharp.UMX
    
    
module Views =
    open Giraffe.ViewEngine
    open FsHotWire.Giraffe

    let private confirmedGameCard (gameName: string<CanonizedGameName>) votes currentUser actionUrl voteUpdateTarget =
        article [ 
            _class "media" 
            _dataGameName %gameName ] [
            figure [ _class "media-left" ] [ 
                p [ _class "image is-64x64" ] [ 
                    img [ _src "http://via.placeholder.com/64" ] 
                ] 
            ]
            div [ _class "media-content" ] [
                div [ _class "content" ] [
                    p [] [ gameName |> GameName.toDisplayName |> str ]
                ]
                nav [ _class "level" ] [ 
                    div [ _class "level-left" ] [
                        yield! GameNightViews.gameVoteButtons currentUser votes actionUrl voteUpdateTarget
                        if GameNightViews.hasVoted votes currentUser then
                            ()
                        else 
                            GameNightViews.addVoteButton actionUrl voteUpdateTarget
                    ]
                ]
            ]
        ]

    let private confirmedGameNightCard (allGames: Map<Guid<GameId>, Game>) currentUser (gn: ConfirmedGameNight) =
        let turboFrameId = "confirmed-game-night-" + gn.Id.ToString()
        turboFrame [ _id turboFrameId ] [
            div [ _class "card mb-5"; _dataGameNightId (gn.Id.ToString()) ] [
                header [ _class "card-header" ] [ 
                    p [ _class "card-header-title" ] [ (gn.CreatedBy |> Username.toDisplayName) + " wants to play" |> str ]
                ]
                div [ _class "card-content" ] [ 
                    for gameId, votes in gn.GameVotes |> NonEmptyMap.toList do
                        let gameName = allGames.[gameId].Name
                        let actionUrl = sprintf "/confirmedgamenight/%A/game/%A/vote" gn.Id %gameId
                        ul [] [
                            li [ ] [
                                confirmedGameCard gameName votes currentUser actionUrl turboFrameId
                            ] 
                        ] 
                    let actionUrl = sprintf "/confirmedgamenight/%A/date/%s/vote" gn.Id gn.Date.AsString
                    GameNightViews.dateCard gn.Date (gn.Players |> NonEmptySet.toSet) currentUser actionUrl turboFrameId
                ]
            ]
        ]
        
    let gameNightsView allGames currentUser confirmed =
        turboFrame [ _id "confirmed-game-nights" ] [
            match confirmed with
            | [] -> 
                emptyText
            | confirmed ->
                section [ _class "section" ] [
                    div [ _class "container" ] [
                        h2 [ _class "title is-2" ] [ str "Confirmed game nights" ]
                        div [] [
                            for gameNight in confirmed do confirmedGameNightCard allGames currentUser gameNight
                        ]
                    ]
                ]
        ]
    
let getAll env : HttpFunc =
    fun ctx -> 
        taskResult {
            let! confirmed = Storage.GameNights.getAllConfirmedGameNights env
            let! currentUser = ctx.GetUser() |> Result.mapError ApiError.MissingUser
            let! allGames = Storage.Games.getAllGames env |> Async.map (Game.toMap)
            return Views.gameNightsView allGames currentUser confirmed 
        } 
        |> (fun view -> ctx.RespondWithHtml(env, Page.GameNights, view))
        
let controller env = controller {
    
    index (getAll env)
}
