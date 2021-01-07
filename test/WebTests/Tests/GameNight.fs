module WebTests.Tests.GameNight

open System
open canopy.classic
open canopy.runner.classic
open WebTests.Pages
open GameNightsPages

let all rootUrl =
    let gameNightUrl = GameNightsPages.gameNightUrl rootUrl
    let username = "Game Night Tester"
    let username2 = "Game Night Tester2"
    let date = (DateTime.Now.AddDays(5.))
    let gameName = sprintf "Web Test Game %s" (date.ToString("yyyy-MM-dd-HHmmss"))
    
    context "game nights"
    
    once (fun _ ->
        LoginPage.login rootUrl username
        waitForElement LoginPage._loggedInUsername)
    
    before (fun _ ->
        url gameNightUrl
        onn gameNightUrl
    )
    
    "Can add proposed game night" &&& fun _ ->
        addProposedGameNight rootUrl gameName date
        let gameNightId = closestGameNightId gameName
        let gnHeader = _gameNightHeader gameNightId 
        let expectedHeader = sprintf "%s wants to play" username 
        expectedHeader == read gnHeader
        displayed gameName
        displayed (date.ToString("yyyy-MM-dd"))
        
    "Can add game vote" &&& fun _ ->
        let gameNightId = closestGameNightId gameName
        click (_addGameVoteButton gameNightId gameName)
        waitForElement (_removeGameVoteButton gameNightId gameName)
        notDisplayed (_addGameVoteButton gameNightId gameName)
        
    "Can remove game vote" &&& fun _ ->
        let gameNightId = closestGameNightId gameName
        click (_removeGameVoteButton gameNightId gameName)
        waitForElement (_addGameVoteButton gameNightId gameName)
        notDisplayed (_removeGameVoteButton gameNightId gameName)
    
    "Can add date vote" &&& fun _ ->
        let gameNightId = closestGameNightId gameName
        click (_addDateVoteButton gameNightId date)
        waitForElement (_removeDateVoteButton gameNightId date)
        notDisplayed (_addDateVoteButton gameNightId date)
        
    "Can remove date vote" &&& fun _ ->
        let gameNightId = closestGameNightId gameName
        click (_removeDateVoteButton gameNightId date)
        waitForElement (_addDateVoteButton gameNightId date)
        notDisplayed (_removeDateVoteButton gameNightId date)
    
    "Cannot remove game vote for other username" &&& fun _ ->
        // as first user
        LoginPage.login rootUrl username
        waitForElement LoginPage._loggedInUsername
        let gameNightId = closestGameNightId gameName
        click (_addGameVoteButton gameNightId gameName)
        waitForElement (_removeGameVoteButton gameNightId gameName)
        
        // as second user:
        LoginPage.login rootUrl username2
        waitForElement LoginPage._loggedInUsername
        notDisplayed (_removeGameVoteButton gameNightId gameName)
    
    "Cannot remove date vote for other username" &&& fun _ ->
        // as first user
        LoginPage.login rootUrl username
        waitForElement LoginPage._loggedInUsername
        let gameNightId = closestGameNightId gameName
        click (_addDateVoteButton gameNightId date)
        waitForElement (_removeDateVoteButton gameNightId date)
        
        // as second user:
        LoginPage.login rootUrl username2
        waitForElement LoginPage._loggedInUsername
        notDisplayed (_removeDateVoteButton gameNightId date)
        
    
