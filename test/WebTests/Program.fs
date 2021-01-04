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
    
    once (fun _ -> LoginPage.logout())
    
    Login.all rootUrl
    
    GameNight.all rootUrl
        
    run()
    
    quit()
    
    failedCount
