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
    let mutable gameNightId = null
    
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
        gameNightId <- closestGameNightId gameName
        read (_gameCardTitle gameNightId gameName) == gameName
        read (_dateCardTitle gameNightId date) == date.ToString("yyyy-MM-dd")
        displayed (date.ToString("yyyy-MM-dd"))
        
    "Uses username display name in game night header" &&& fun _ ->
        let gnHeader = _gameNightHeader gameNightId 
        let expectedHeader = sprintf "%s wants to play" username 
        expectedHeader == read gnHeader
        
    "Can add game vote" &&& fun _ ->
        click (_addGameVoteButton gameNightId gameName)
        waitForElement (_removeGameVoteButton gameNightId gameName)
        notDisplayed (_addGameVoteButton gameNightId gameName)
        
    "Uses username display name on remove game vote button" &&& fun _ ->
        (_removeGameVoteButton gameNightId gameName) == username
        
    "Can remove game vote" &&& fun _ ->
        click (_removeGameVoteButton gameNightId gameName)
        waitForElement (_addGameVoteButton gameNightId gameName)
        notDisplayed (_removeGameVoteButton gameNightId gameName)
    
    "Can add date vote" &&& fun _ ->
        click (_addDateVoteButton gameNightId date)
        waitForElement (_removeDateVoteButton gameNightId date)
        notDisplayed (_addDateVoteButton gameNightId date)
        
    "Uses username display name on remove date vote button" &&& fun _ ->
        (_removeDateVoteButton gameNightId date) == username
        
    "Can remove date vote" &&& fun _ ->
        click (_removeDateVoteButton gameNightId date)
        waitForElement (_addDateVoteButton gameNightId date)
        notDisplayed (_removeDateVoteButton gameNightId date)
    
    "Cannot remove game vote for other username" &&& fun _ ->
        // as first user
        LoginPage.login rootUrl username
        waitForElement LoginPage._loggedInUsername
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
        click (_addDateVoteButton gameNightId date)
        waitForElement (_removeDateVoteButton gameNightId date)
        
        // as second user:
        LoginPage.login rootUrl username2
        waitForElement LoginPage._loggedInUsername
        notDisplayed (_removeDateVoteButton gameNightId date)
        
    
