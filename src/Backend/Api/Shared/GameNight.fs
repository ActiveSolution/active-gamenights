[<RequireQualifiedAccess>]
module Backend.Api.Shared.GameNight

open Feliz.Bulma.ViewEngine
open Feliz.ViewEngine
open Backend.Api.Shared
open Domain
open Backend.Turbo

let private addVoteButton actionUrl =
    Bulma.levelItem [
        Html.form [
            prop.method "POST"
            prop.action actionUrl
            prop.children [
                Partials.fieldControl [
                    Bulma.button.button [
                        color.isPrimary
                        prop.type'.submit
                        button.isSmall
                        prop.text "add vote"
                    ]
                ]
            ]
        ]
    ]
    
let addGameVoteButton (gameNightId: GameNightId) (gameName: GameName) =
    addVoteButton (sprintf "/gamenight/%s/game/%s/vote" (gameNightId.Val.ToString()) gameName.Canonized)
    
let addDateVoteButton (gameNightId: GameNightId) (date: Date) =
    addVoteButton (sprintf "/gamenight/%s/date/%s/vote" (gameNightId.Val.ToString()) date.AsString)
    
let private removeVoteButton actionUrl (user: User) =
    Bulma.levelItem [
        Html.form [
            prop.method "POST"
            prop.action actionUrl
            prop.children [
                Html.input [
                    prop.type'.hidden
                    prop.name "_method"
                    prop.value "delete"
                ]
                Partials.fieldControl [    
                    Bulma.button.button [
                        color.isInfo
                        prop.type'.submit
                        prop.custom ("onmouseover","this.style.backgroundColor='#feecf0';this.style.color='#cc0f35';")
                        prop.custom ("onmouseout","this.style.backgroundColor='#3298dc';this.style.color='white'")
                        button.isSmall
                        prop.text user.Val
                    ]
                ]
            ]
        ]
    ]
    
let removeGameVoteButton (gameNightId : GameNightId) (gameName: GameName) (user: User) =
    removeVoteButton (sprintf "/gamenight/%s/game/%s/vote/%s" (gameNightId.Val.ToString()) gameName.Canonized user.Canonized) user
    
let removeDateVoteButton (gameNightId : GameNightId) (date: Date) (user: User) =
    removeVoteButton (sprintf "/gamenight/%s/date/%s/vote/%s" (gameNightId.Val.ToString()) date.AsString user.Canonized) user
    
let otherUsersVoteButton (user: User) =
    Bulma.levelItem [ 
        Partials.fieldControl [    
            Bulma.button.button [
                prop.classes [ "no-hover" ]
                color.isPrimary
                button.isSmall
                prop.text user.Val
            ]
        ]
    ]
    
let gameVoteButtons (gameNightId: GameNightId) (gameName: GameName) (currentUser: User) (votes: Set<User>) = [
    for user in Set.toList votes do
        if user.Val = currentUser.Val then
            removeGameVoteButton gameNightId gameName user 
        else
            otherUsersVoteButton user
    ]
    
let private dateVoteButtons (gameNightId: GameNightId) date (currentUser: User) votes = [
    for (user: User) in Set.toList votes do
        if user = currentUser then
            removeDateVoteButton gameNightId date user
        else
            otherUsersVoteButton user
    ]
    
let private hasVoted votes user =
    votes
    |> Set.contains user
    
let gameCard gameNightId (gameName: GameName) votes currentUser =
    Bulma.media [
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
                Html.p gameName.Val
            ]
            Bulma.level [
                Bulma.levelLeft [
                    yield! gameVoteButtons gameNightId gameName currentUser votes
                    if hasVoted votes currentUser then
                        Html.none
                    else
                        addGameVoteButton gameNightId gameName 
                ]
            ]
        ]
    ]

let dateCard gameNightId (date: Date) votes currentUser =
    Bulma.media [
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
                Html.p (date |> Date.asString)
            ]
            Bulma.level [
                Bulma.levelLeft [
                    yield! dateVoteButtons gameNightId date currentUser votes
                    if hasVoted votes currentUser then
                        Html.none
                    else
                        addDateVoteButton gameNightId date
                ]
            ]
        ]
    ]
