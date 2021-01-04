module WebTests.Pages.GameNightPages

open System
open canopy.classic
open WebTests.Extensions

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
let _gameInput index = sprintf "#game-%i" index |> css
let _dateInput index = sprintf "#date-%i" index |> css
let _addProposedGameNightLink = "#add-proposed-game-night-link" |> css
let _gameCard gameName = sprintf ".game-card[data-game-name=%s]" (canonize gameName) |> css
let _parentGameNightCard gameName =
    let gameNightCard = XPath.containsClass "game-night-card"
    let containsGameName = XPath.containsText gameName
    sprintf "//*[%s]//ancestor::div[%s][1]" containsGameName gameNightCard |> xpath

let _dateCard gameName date =
    let dateCardSelector = sprintf ".date-card[data-date='%s']" (toDateString date)
    let gameNightCard = (_parentGameNightCard gameName)
    element gameNightCard |> someElementWithin dateCardSelector
let _gameNightCard gameName = sprintf ".game-card[data-game-name='%s']" (canonize gameName) |> css

let _someAddGameVoteButton gameName = element (_gameCard gameName) |> someElementWithin "add vote"
let _someRemoveGameVoteButton gameName username = element (_gameCard gameName) |> someElementWithin username
let _someAddDateVoteButton gameName date = (_dateCard gameName date) |> Option.bind (fun el -> el |> someElementWithin "add vote")
let _someRemoveDateVoteButton gameName date username = (_dateCard gameName date) |> Option.bind (fun el -> el |> someElementWithin username)

let addProposedGameNight rootUrl game (date: DateTime) =
    describe (sprintf "Adding proposed game night: game %s for date %s" game (toDateString date))
    
    url (gameNightUrl rootUrl)
    waitForElement _addProposedGameNightLink
    click _addProposedGameNightLink
    waitForElement (_gameInput 1)
    (_gameInput 1) << game
    (_dateInput 1) << (toDateString date)
    click "Save"
    
