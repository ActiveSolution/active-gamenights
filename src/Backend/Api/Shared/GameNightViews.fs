[<RequireQualifiedAccess>]
module Backend.Api.Shared.GameNightViews

open Backend.Extensions
open Giraffe.ViewEngine
open Backend.Api.Shared
open Domain
open FsHotWire.Giraffe
open Backend.Extensions
open FSharp.UMX
open System

let addVoteButton addVoteUrl target =
    div [ _class "level-item" ]
        [
            form 
                [ 
                    _targetTurboFrame target 
                    _method "POST"
                    _action addVoteUrl
                ] 
                [
                    div [ _class "field" ]
                        [
                            div [ _class "control" ]
                                [
                                    button 
                                        [
                                            yield! Stimulus.loadingButton "is-loading"
                                            _addVoteButton 
                                            _class "button is-primary is-small"
                                            _type "submit"
                                        ] 
                                        [ "add vote" |> str ]
                                ]
                        ]
                ]
        ]
    
let removeVoteButton removeVoteUrl (user: string<CanonizedUsername>) target =

    div [ _class "level-item" ]
        [
            form 
                [ 
                    _targetTurboFrame target 
                    _method "POST"
                    _action (removeVoteUrl + "/" + %user)
                ] 
                [
                    input [
                        _type "hidden"
                        _name "_method"
                        _value "delete"
                    ]
                    div [ _class "field" ]
                        [
                            div [ _class "control" ]
                                [
                                    button 
                                        [
                                            yield! Stimulus.loadingButton "is-loading"
                                            _removeVoteButton 
                                            _class "button is-info is-small"
                                            _type "submit"
                                            _onmouseover "this.style.backgroundColor='#feecf0';this.style.color='#cc0f35';"
                                            _onmouseout "this.style.backgroundColor='#3298dc';this.style.color='white'"
                                        ] 
                                        [ user |> Username.toDisplayName |> str ]
                                ]
                        ]
                ]
        ]
    
let otherUsersVoteButton (user: string<CanonizedUsername>) =
    div [ _class "level-item" ] [ 
        div [ _class "field"] [
            div [ _class "control" ] [ 
                button [ 
                    _class "button is-small is-primary no-hover" 
                    _disabled
                ] [ user |> Username.toDisplayName |> str ]
            ]
        ]
    ]
    
let gameVoteButtons (currentUser: string<CanonizedUsername>) votes removeVoteUrl target = [
    for user in Set.toList votes do
        if user = currentUser then
            removeVoteButton removeVoteUrl currentUser target 
        else
            otherUsersVoteButton user
    ]
    
let private dateVoteButtons (currentUser: string<CanonizedUsername>) votes removeVoteUrl target = [
    for user in Set.toList votes do
        if user = currentUser then
            removeVoteButton removeVoteUrl currentUser target
        else
            otherUsersVoteButton user
    ]
    
let hasVoted votes user =
    votes
    |> Set.contains user
    

let dateCard (date: DateTime) votes currentUser actionUrl voteUpdateTarget =
    article [
        _class "media"
        _dataDate date.AsString
    ] [
        figure [ _class "media-left" ] [ 
            p [ _class "image is-64x64" ] [ 
                img [ _src "/Images/calendar.jpg" ]  
            ] 
        ]
        div [ _class "media-content" ] [
            div [ _class "content" ] [
                p [] [ date.AsString |> str ]
            ]
            nav [ _class "level" ] [ 
                div [ _class "level-left" ] [
                    yield! dateVoteButtons currentUser votes actionUrl voteUpdateTarget
                    if hasVoted votes currentUser then
                        ()
                    else 
                        addVoteButton actionUrl voteUpdateTarget
                ]
            ]
        ]
    ]