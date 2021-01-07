[<RequireQualifiedAccess>]
module Backend.Api.Shared.GameNightViews

open Feliz.Bulma.ViewEngine
open Feliz.ViewEngine
open Backend.Api.Shared
open Domain
open FsHotWire.Feliz
open Backend.Extensions
open FSharp.UMX

let addVoteButton addVoteUrl target =
    Bulma.levelItem [
        Html.form [
            prop.targetTurboFrame target
            prop.method "POST"
            prop.action addVoteUrl
            prop.children [
                Bulma.fieldControl [
                    Bulma.button.button [
                        prop.addVoteButton
                        color.isPrimary
                        prop.type'.submit
                        button.isSmall
                        prop.text "add vote"
                    ]
                ]
            ]
        ]
    ]
    
let removeVoteButton removeVoteUrl (user: string<CanonizedUsername>) target =
    Bulma.levelItem [
        Html.form [
            prop.targetTurboFrame target
            prop.method "POST"
            prop.action (removeVoteUrl + "/" + %user)
            prop.children [
                Html.input [
                    prop.type'.hidden
                    prop.name "_method"
                    prop.value "delete"
                ]
                Bulma.fieldControl [    
                    Bulma.button.button [
                        prop.removeVoteButton
                        color.isInfo
                        prop.type'.submit
                        prop.custom ("onmouseover","this.style.backgroundColor='#feecf0';this.style.color='#cc0f35';")
                        prop.custom ("onmouseout","this.style.backgroundColor='#3298dc';this.style.color='white'")
                        button.isSmall
                        prop.text (user |> Username.toDisplayName)
                    ]
                ]
            ]
        ]
    ]
    
let otherUsersVoteButton (user: string<CanonizedUsername>) =
    Bulma.levelItem [ 
        Bulma.fieldControl [    
            Bulma.button.button [
                prop.classes [ "no-hover" ]
                prop.disabled true
                color.isPrimary
                button.isSmall
                prop.text %user
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
    
let private hasVoted votes user =
    votes
    |> Set.contains user
    
let gameCard (gameName: string<CanonizedGameName>) votes currentUser actionUrl voteUpdateTarget =
    Bulma.media [
        prop.dataGameName gameName
        prop.children [
            Bulma.mediaLeft [
                Bulma.image [
                    image.is64x64
                    prop.children [
                        Html.img [
                            prop.src "http://via.placeholder.com/64"
                        ]
                    ]
                ]
            ]
            Bulma.mediaContent [
                Bulma.content [
                    Html.p (GameName.toDisplayName gameName)
                ]
                Bulma.level [
                    Bulma.levelLeft [
                        yield! gameVoteButtons currentUser votes actionUrl voteUpdateTarget 
                        if hasVoted votes currentUser then
                            Html.none
                        else
                            addVoteButton actionUrl voteUpdateTarget
                    ]
                ]
            ]
        ]
    ]

let dateCard date votes currentUser actionUrl voteUpdateTarget =
    Bulma.media [
        prop.dataDate date
        prop.children [
            Bulma.mediaLeft [
                Bulma.image [
                    image.is64x64
                    prop.children [
                        Html.img [
                            prop.src "/Images/calendar.jpg"
                        ]
                    ]
                ]
            ]
            Bulma.mediaContent [
                Bulma.content [
                    Html.p (date.AsString)
                ]
                Bulma.level [
                    Bulma.levelLeft [
                        yield! dateVoteButtons currentUser votes actionUrl voteUpdateTarget
                        if hasVoted votes currentUser then
                            Html.none
                        else
                            addVoteButton actionUrl voteUpdateTarget 
                    ]
                ]
            ]
        ]
    ]
