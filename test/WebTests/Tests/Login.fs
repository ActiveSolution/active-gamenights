module WebTests.Tests.Login

open canopy.classic
open canopy.runner.classic
open WebTests.Pages

let all rootUrl =
    let loginUrl = LoginPage.url rootUrl
    
    url loginUrl
    
    "Can login with any username" &&& fun _ ->
        "[name=username]" << "test name"
        click "[name=submit]"       
        displayed "Test Name"