open System
open WebTests.Pages
open WebTests.Tests
open canopy.classic
open canopy.runner.classic
open canopy.types
   
canopy.configuration.failFast := true
canopy.configuration.firefoxDir <- "/Applications/Firefox\ Developer\ Edition.app"

[<EntryPoint>]
let main args =

    let rootUrl = args |> Array.tryHead |> Option.defaultValue "http://localhost:8085"
    let failIfAnyWipTests = if args.Length = 2 then Convert.ToBoolean(args.[1]) else false
    canopy.configuration.failIfAnyWipTests <- failIfAnyWipTests
    start BrowserStartMode.ChromeHeadless
//    start BrowserStartMode.Chrome
    resize (1920, 1080)
    
    "Root url redirects login page" &&& fun _ ->
        let loginPage = LoginPage.url rootUrl
        url rootUrl
        onn loginPage
    
    Login.all rootUrl

//    "Health endpoint returns time in timezone /Europe/Stockholm" &&& fun _ ->
//        url (rootUrl + "/health")
//        let actual = read "#healthCheckTime"
//        let expected = DateTime.Now.ToString("yyyy-MM-dd hh:mm")
//        contains expected actual
        
    run()
    
    quit()
    
    failedCount
