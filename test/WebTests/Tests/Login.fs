module WebTests.Tests.Login

open canopy.classic
open canopy.runner.classic
open WebTests.Pages
open LoginPage

let all rootUrl =
    context "login page"
    
    "Root url redirects login page" &&& fun _ ->
        notDisplayed _loggedInUsername
        let loginPage = _loginUrl rootUrl
        url rootUrl
        onn loginPage
        url (GameNightPages.gameNightUrl rootUrl)
        onn loginPage
    
    
    "Can login using just a username" &&& fun _ ->
        notDisplayed _loggedInUsername
        url (_loginUrl rootUrl)
        login rootUrl "test name"
        _loggedInUsername == "Test Name"
        
    "Can logout" &&& fun _ ->
        displayed _loggedInUsername
        logout()
        notDisplayed _loggedInUsername
        notDisplayed "Test Name"
