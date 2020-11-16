module Backend.Browser.GameNight.Views

open Backend.Browser.Common.View.Helpers
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine
open Domain
open FSharpPlus.Data


let private addVoteButton className dataAttrs (text :string) =
    Bulma.levelItem [
        Bulma.tag [
            for dataAttr in dataAttrs do
                prop.custom dataAttr
            color.isPrimary
            prop.classes [ className; "button is-light" ]
            prop.text text
        ]
    ]
    
let private removeVoteButton className dataAttrs (text :string) =
    Bulma.levelItem [
        Bulma.tag [
            for dataAttr in dataAttrs do
                prop.custom dataAttr
            color.isPrimary
            prop.text text
            prop.classes [ className; "button" ]
            prop.custom ("onmouseover","this.style.backgroundColor='#feecf0';this.style.color='#cc0f35';")
            prop.custom ("onmouseout","this.style.backgroundColor='#00d1b2';this.style.color='white'")
        ]
    ]
    
let private gameVoteList (gameNightId: GameNightId) (GameName gameName) (currentUser: User) votes = [
    for (User name) in Set.toList votes do
        if name = currentUser.Val then
            removeVoteButton "remove-game-vote-button" [("data-username", currentUser.Val); ("data-game", gameName); ("data-gamenight", gameNightId.Val.ToString())] currentUser.Val
        else
            Bulma.levelItem [ 
                Bulma.tag [
                    color.isPrimary
                    prop.text name
                ] 
            ]
    ]

let hasVoted votes user =
    votes
    |> Set.contains user
    
let private dateVoteList (gameNightId: GameNightId) date (currentUser: User) votes = [
    for (user: User) in Set.toList votes do
        if user = currentUser then
            removeVoteButton "remove-date-vote-button" ["data-date", (date |> Date.toString); ("data-gamenight", gameNightId.Val.ToString())] currentUser.Val
        else
            Bulma.levelItem [ 
                Bulma.tag [
                    color.isPrimary
                    prop.text (user.Val)
                ] 
            ]
    ]
    
let private gameNightCard currentUser (gn: ProposedGameNight) =
    let gameCard (gameName: GameName) votes =
        Bulma.media [
            Bulma.mediaLeft [
                Bulma.image [
                    image.is64x64
                    prop.children [
                        Html.img [
                            prop.src "https://via.placeholder.com/64"
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
                        yield! gameVoteList gn.Id gameName currentUser votes
                        if hasVoted gn.GameVotes.[gameName] currentUser then
                            Html.none
                        else
                            addVoteButton "add-game-vote-button" [("data-game", gameName.Val); ("data-gamenight", gn.Id.Val.ToString())] "add vote"
                    ]
                ]
            ]
        ]
        
    let dateCard (date: Date) votes =
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
                    Html.p (date |> Date.toString)
                ]
                Bulma.level [
                    Bulma.levelLeft [
                        yield! dateVoteList gn.Id date currentUser votes
                        if hasVoted gn.DateVotes.[date] currentUser then
                            Html.none
                        else
                            addVoteButton "add-date-vote-button" ["data-date", (date |> Date.toString); ("data-gamenight", gn.Id.Val.ToString())] "add vote"
                    ]
                ]
            ]
        ]
    
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
                            gameCard gameName votes 
                        ] 
                    ] 
                for date, votes in gn.DateVotes |> NonEmptyMap.toList do
                    Html.unorderedList [
                        Html.listItem [
                            dateCard date votes 
                        ] 
                    ]
            ]
        ]
    ]

let private proposedGameNightsList currentUser gameNights =
    gameNights
    |> List.map (gameNightCard currentUser)
    |> Bulma.section
    
    
let private addProposedGameLink =
    Html.a [
        prop.href "/gamenight/add"
        prop.children [ plusIcon; Html.text "Add new game night" ]
    ]
    
let proposedGameNightsView currentUser gameNights =
    Bulma.container [
        Bulma.title.h2 "Vote for upcoming game nights"
        Bulma.section [
            for gameNight in gameNights do gameNightCard currentUser gameNight
        ]
        addProposedGameLink
    ]

let addProposedGameNightView =
    Bulma.section [
        Bulma.subtitle.h3 "What do you want to play?"
        for i in 1..3 do
            fieldLabelControl "Game" [
                Html.input [
                    prop.type'.text
                    prop.classes [ "input" ]
                    prop.id (sprintf "create-game-night-game%i" i)
                    prop.placeholder "Enter a game"
                ]
            ]
        Bulma.subtitle.h3 "When?"
        for i in 1..3 do
            fieldLabelControl "Date" [
                Html.input [
                    prop.type'.date
                    prop.classes [ "input" ]
                    prop.id (sprintf "create-game-night-date%i" i)
                    prop.placeholder "Pick a date"
            ]
        ] 
        Bulma.button.button [
            prop.id "create-game-night-button"
            color.isPrimary
            prop.type'.submit
            prop.text "Save"
        ]   
    ]
