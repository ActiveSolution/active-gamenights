module WebTests.Tests.GameNight

open System
open canopy.classic
open canopy.runner.classic
open WebTests.Pages
open GameNightPages
open WebTests.Extensions

let all rootUrl =
    let gameNightUrl = GameNightPages.gameNightUrl rootUrl
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
        displayed gameName
        displayed (date.ToString("yyyy-MM-dd"))
        
    "Can add game vote" &&& fun _ ->
        click (_someAddGameVoteButton gameName).Value
        waitForElementOption (fun _ -> _someRemoveGameVoteButton gameName username)
        notDisplayedOption (_someAddGameVoteButton gameName)
        
    "Can remove game vote" &&& fun _ ->
        click (_someRemoveGameVoteButton gameName username).Value
        waitForElementOption (fun _ -> _someAddGameVoteButton gameName)
        notDisplayedOption (_someRemoveGameVoteButton gameName username) 
    
    "Can add date vote" &&& fun _ ->
        click (_someAddDateVoteButton gameName date).Value
        waitForElementOption (fun _ -> _someRemoveDateVoteButton gameName date username)
        notDisplayedOption (_someAddDateVoteButton gameName date)
        
    "Can remove date vote" &&& fun _ ->
        click (_someRemoveDateVoteButton gameName date username).Value
        waitForElementOption (fun _ -> _someAddDateVoteButton gameName date)
        notDisplayedOption (_someRemoveDateVoteButton gameName date username) 
    
    "Cannot remove game vote for other username" &&& fun _ ->
        // as first user
        LoginPage.login rootUrl username
        click (_someAddGameVoteButton gameName).Value
        waitForElementOption (fun _ -> _someRemoveGameVoteButton gameName username)
        
        // as second user:
        LoginPage.login rootUrl username2
        waitForElement LoginPage._loggedInUsername
        let button = (_someRemoveGameVoteButton gameName username).Value
        containsInsensitive (button.GetProperty "disabled") "true"
    
    "Cannot remove date vote for other username" &&& fun _ ->
        // as first user
        LoginPage.login rootUrl username
        click (_someAddDateVoteButton gameName date).Value
        waitForElementOption (fun _ -> _someRemoveDateVoteButton gameName date username)
        
        // as second user:
        LoginPage.login rootUrl username2
        waitForElement LoginPage._loggedInUsername
        let button = (_someRemoveDateVoteButton gameName date username).Value
        containsInsensitive (button.GetProperty "disabled") "true"
        
    
