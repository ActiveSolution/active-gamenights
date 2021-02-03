module WebTests.Pages.AddPropoposedGameNightPage

open canopy.classic
open WebTests.Extensions


let uri rootUrl = rootUrl + "/proposedgamenight/add"
let _gameSelect index = sprintf "#game-select-%i" index |> css
let _dateInput index = sprintf "#date-input-%i" index |> css
