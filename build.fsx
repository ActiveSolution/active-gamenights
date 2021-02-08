#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "Facades/netstandard"
#r "netstandard"
#endif
open System
open Fake.Core
open Fake.DotNet
open Fake.Tools
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

module Tools =
    let platformTool tool winTool =
        let tool = if Environment.isUnix then tool else winTool
        match ProcessUtils.tryFindFileOnPath tool with
        | Some t -> t
        | _ ->
            let errorMsg =
                tool + " was not found in path. " +
                "Please install it and make sure it's available from your path. " +
                "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
            failwith errorMsg

    let private yarnTool = platformTool "yarn" "yarn.exe"
    let private createProcess cmd args workingDir =   
        let arguments = args |> Arguments.OfArgs
        Command.RawCommand (cmd, arguments)
        |> CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory workingDir
        |> CreateProcess.ensureExitCode
    let private runTool cmd args workingDir = 
        createProcess cmd args workingDir 
        |> Proc.run
        |> ignore

    let yarn (args: string list) = runTool yarnTool args

type DeployEnvironment =
    | Test
    | Prod
let sln = "ActiveGameNight.sln"
let rootPath = __SOURCE_DIRECTORY__
let src = rootPath @@ "src"
let test = rootPath @@ "test"
let srcGlob = src @@ "**/*.??proj"
let deployGlob = rootPath  @@ "deploy/**/*.??proj"
let backendPath = src @@ "Backend"
let webTestsPath = test @@ "WebTests"
let backendProj = backendPath @@ "Backend.fsproj"
let farmerDeployPath = rootPath @@ "deploy" @@ "farmer"
let jsEntryPath = backendPath @@ "scripts" @@ "application.js" |> Path.getFullName
let jsOutputPath = backendPath @@ "public" @@ "Scripts" |> Path.getFullName
let outputPath = "./output"
let webAppOutput = outputPath @@ "webapp"
let testSite = "https://active-game-night-test.azurewebsites.net"
let prodSite = "https://active-game-night.azurewebsites.net"

//-----------------------------------------------------------------------------
// Helpers
//-----------------------------------------------------------------------------
let invokeAsync f = async { f () }

let isRelease (targets : Target list) =
    targets
    |> Seq.map(fun t -> t.Name)
    |> Seq.exists ((=)"CreateRelease")

let configuration (targets : Target list) =
    let defaultVal = if isRelease targets then "Release" else "Debug"
    match Environment.environVarOrDefault "CONFIGURATION" defaultVal with
    | "Debug" -> DotNet.BuildConfiguration.Debug
    | "Release" -> DotNet.BuildConfiguration.Release
    | config -> DotNet.BuildConfiguration.Custom config

let dotNetWatch watchCmd workingDir args =
    DotNet.exec
        (fun p -> { p with WorkingDirectory = workingDir })
        (sprintf "watch %s" watchCmd)
        args
    |> ignore

let semVersion = 
    let changelog = Changelog.load "CHANGELOG.md"
    changelog.LatestEntry.SemVer

    
//-----------------------------------------------------------------------------
// Build Target Implementations
//-----------------------------------------------------------------------------

let clean _ =
    [ "bin"; "temp"; outputPath; jsOutputPath ]
    |> Shell.cleanDirs

    !! srcGlob
    ++ deployGlob
    |> Seq.collect(fun p ->
        ["bin";"obj"]
        |> Seq.map(fun sp ->
            IO.Path.GetDirectoryName p @@ sp)
        )
    |> Shell.cleanDirs

    [ "paket-files/paket.restore.cached" ]
    |> Seq.iter Shell.rm


let dotnetPublishBackend ctx =
    Tools.yarn [ "run"; "parcel"; "build"; jsEntryPath; "--out-dir"; jsOutputPath; "--out-file"; "bundle.js"; "--public-url"; "/Scripts" ] rootPath

    Shell.copyDir (webAppOutput @@ "public") (backendPath @@ "public") (fun _ -> true)

    let args =
        [
            sprintf "/p:PackageVersion=%s" (semVersion).AsString
        ]
    DotNet.publish(fun c ->
        { c with
            Configuration = configuration (ctx.Context.AllExecutingTargets)
            NoBuild = true
            NoRestore = true
            SelfContained = Some false
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
            OutputPath = Some (webAppOutput)
        }) backendProj

let dotnetRestore _ = DotNet.restore id sln
let yarnInstall _ = Tools.yarn [] rootPath

let writeVersionToFile _ =
    let sb = System.Text.StringBuilder("module Backend.Version\n\n")
    Printf.bprintf sb "    let version = \"%s\"\n" (semVersion.AsString)

    File.writeString false (backendPath @@ "Version.fs") (sb.ToString())
    
let dotnetBuild ctx =
    let args =
        [
            sprintf "/p:PackageVersion=%s" semVersion.AsString
        ]
    DotNet.build(fun c ->
        { c with
            Configuration = configuration (ctx.Context.AllExecutingTargets)
            NoRestore = true
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
        }) sln

let watchApp _ =

    let frontEnd() = Tools.yarn [ "run"; "parcel"; jsEntryPath; "--out-dir"; jsOutputPath; "--out-file"; "bundle.js"; "--public-url"; "/Scripts" ] rootPath
    let server() = dotNetWatch "run" backendPath ""

    [ frontEnd; server ]
    |> Seq.iter (invokeAsync >> Async.Catch >> Async.Ignore >> Async.Start)
    printfn "Press Ctrl+C (or Ctrl+Break) to stop..."
    let cancelEvent = Console.CancelKeyPress |> Async.AwaitEvent |> Async.RunSynchronously
    cancelEvent.Cancel <- true

let runFarmerDeploy env _ =
    let args = 
        match env with
        | Test ->
            sprintf "--project %s -- deployEnvironment=test" farmerDeployPath
        | Prod -> 
            sprintf "--project %s -- deployEnvironment=prod" farmerDeployPath
    DotNet.exec id "run" args
    |> (fun p -> if p.ExitCode <> 0 then failwith "Farmer deployment failed")

let gitCheckUniqueTag _ =
    let allTags = 
        match Git.CommandHelper.runGitCommand "" "tag" with
        | false, _,_ -> 
            failwith "git error"
        | true, tags, _ -> 
            tags 
    let currentVersion = semVersion.AsString
    Trace.tracefn "Current version is %s" currentVersion
    Trace.tracefn "Existing tags:"
    allTags
    |> List.iter (Trace.tracefn "%s")
    if allTags |> List.contains currentVersion then failwithf "git tag already exists for %s, make sure ChangeLog is updated with a new version for this release" currentVersion

let gitTagDeployment _ =
    let tag = semVersion.AsString
    Git.Branches.tag "" tag
    Git.Branches.pushTag "" "origin" tag

let waitForDeployment env _ =
    let waitForResponse timeout url =
        let sw = System.Diagnostics.Stopwatch.StartNew()
        let mutable lastExceptionMsg = ""
        let rec waitForResponse'() =
            if sw.Elapsed < timeout then
                try 
                    let response = Fake.Net.Http.get "" "" url
                    if response.Contains("Version: " + semVersion.AsString) |> not then failwith "Site is not up"
                    Trace.tracefn "Site %s responded after %f s" url sw.Elapsed.TotalSeconds
                with exn ->
                    lastExceptionMsg <- exn.ToString()
                    Trace.tracefn "Site %s not responding after %f s, waiting..." url sw.Elapsed.TotalSeconds
                    System.Threading.Thread.Sleep 2000
                    waitForResponse'()
            else 
                Trace.traceErrorfn "%O" lastExceptionMsg 
                failwithf "Site %s is not running after %f s" url timeout.TotalSeconds
        waitForResponse'()

    Trace.tracefn "Waiting 5 seconds before warmup tests..."
    System.Threading.Thread.Sleep 5000
    match env with
    | Test ->
        waitForResponse (TimeSpan.FromSeconds(120.)) (testSite + "/version")
    | Prod ->
        waitForResponse (TimeSpan.FromSeconds(120.)) (prodSite + "/version")

let runWebTests ctx =
    let configuration = (configuration ctx.Context.AllExecutingTargets).ToString()
    let args = sprintf "--configuration %s --no-restore --no-build -- %s true" (configuration.ToString()) testSite
    DotNet.exec (fun c ->
        { c with WorkingDirectory = webTestsPath }) "run" args
    |> (fun res -> if not res.OK then failwithf "RunWebTests failed")



//-----------------------------------------------------------------------------
// Build Target Declaration
//-----------------------------------------------------------------------------

Target.create "Clean" clean
Target.create "DotnetRestore" dotnetRestore
Target.create "YarnInstall" yarnInstall
Target.create "WriteVersionToFile" writeVersionToFile
Target.create "DotnetBuild" dotnetBuild
Target.create "WatchApp" watchApp
Target.create "Build" ignore
Target.create "DotnetPublishBackend" dotnetPublishBackend
Target.create "Package" ignore
Target.create "DeployToTest" (runFarmerDeploy Test)
Target.create "DeployToProd" (runFarmerDeploy Prod)
Target.create "GitCheckVersionTag" gitCheckUniqueTag
Target.create "GitTagProdDeployment" gitTagDeployment
Target.create "WaitForTestDeployment" (waitForDeployment Test)
Target.create "RunWebTests" runWebTests
Target.create "WaitForProdDeployment" (waitForDeployment Prod)
Target.create "CreateTestRelease" ignore
Target.create "CreateProdRelease" ignore


//-----------------------------------------------------------------------------
// Build Target Dependencies
//-----------------------------------------------------------------------------

// Only call Clean if 'Package' was in the call chain
// Ensure Clean is called before 'DotnetRestore' and 'InstallClient'
"Clean" ?=> "DotnetRestore"
"Clean" ?=> "YarnInstall"
"Clean" ==> "Package"

"WriteVersionToFile"
    ?=> "WatchApp"

"DotnetRestore"
    ==> "WriteVersionToFile"
    ==> "DotnetBuild" <=> "YarnInstall"
    ==> "Build"
    ==> "DotnetPublishBackend"
    ==> "Package"

"DeployToTest"
    ==> "WaitForTestDeployment"
    // ==> "RunWebTests" // web tests are missing game-tests
    ==> "CreateTestRelease"

"GitCheckVersionTag"
    ==> "Package"
    ==> "DeployToProd"
    ==> "GitTagProdDeployment"
    ==> "WaitForProdDeployment"
    ==> "CreateProdRelease"

//-----------------------------------------------------------------------------
// Start
//-----------------------------------------------------------------------------

Target.runOrDefaultWithArguments "Build"
