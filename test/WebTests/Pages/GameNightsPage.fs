module WebTests.Pages.GameNightsPages

open System
open OpenQA.Selenium
open canopy.classic

let private replaceDelimiter (oldDelim: char) newDelim (str: String) =
    str.Split(oldDelim)
    |> Seq.map (
        Seq.toList >>
        (function
            | first::rest -> Char.ToUpper(first) :: rest
            | [] -> []) >>
        (Array.ofList >> String))
    |> (fun strings -> String.Join(newDelim, strings))

let private canonize = replaceDelimiter ' ' "_"
let private toDateString (date: DateTime) = date.ToString("yyyy-MM-dd")
let gameNightUrl rootUrl = rootUrl + "/gamenight"

let _addProposedGameNightLink = "#add-proposed-game-night-link"
let _gameCard gameName = sprintf "[data-game-name=%s]" (canonize gameName)
let _addGameVoteButton (gameNightId) (gameName) = sprintf "[data-game-night-id=%s] [data-game-name=%s] [data-add-vote-button]" gameNightId (canonize gameName)
let _removeGameVoteButton (gameNightId) (gameName) = sprintf "[data-game-night-id=%s] [data-game-name=%s] [data-remove-vote-button]" gameNightId (canonize gameName)
let _addDateVoteButton gameNightId date = sprintf "[data-game-night-id=%s] [data-date=%s] [data-add-vote-button]" gameNightId (toDateString date)
let _removeDateVoteButton gameNightId date = sprintf "[data-game-night-id=%s] [data-date=%s] [data-remove-vote-button]" gameNightId (toDateString date)

let _dateCard gameNightId date = sprintf "[data-game-night-id=%s][data-date=%s]" gameNightId (toDateString date) 

let addProposedGameNight rootUrl game (date: DateTime) =
    describe (sprintf "Adding proposed game night: game %s for date %s" game (toDateString date))
    
    url (gameNightUrl rootUrl)
    waitForElement _addProposedGameNightLink
    click _addProposedGameNightLink
    waitForElement (AddPropoposedGameNightPage._gameInput 1)
    (AddPropoposedGameNightPage._gameInput 1) << game
    (AddPropoposedGameNightPage._dateInput 1) << (toDateString date)
    click "Save"
    
    
let private tryGetAttribute key (el: IWebElement) =
    let value = el.GetAttribute key
    if String.IsNullOrWhiteSpace value then None else Some value
        
let closestGameNightId gameName =
    let rec getGameNightId (el: IWebElement) =
        match tryGetAttribute "data-game-night-id" el with
        | Some value -> value
        | None -> el |> parent |> getGameNightId
    
    element (_gameCard gameName)
    |> getGameNightId