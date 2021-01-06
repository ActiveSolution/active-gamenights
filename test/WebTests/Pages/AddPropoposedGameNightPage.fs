module WebTests.Pages.AddPropoposedGameNightPage

open canopy.classic
open WebTests.Extensions


let uri rootUrl = rootUrl + "/proposedgamenight/add"
let _gameInput index = sprintf "#game-%i" index |> css
let _dateInput index = sprintf "#date-%i" index |> css
