open System.IO
open Farmer
open Farmer.Builders
open System

// Create ARM resources here e.g. webApp { } or storageAccount { } etc.
// See https://compositionalit.github.io/farmer/api-overview/resources/ for more details.

// Add resources to the ARM deployment using the add_resource keyword.
// See https://compositionalit.github.io/farmer/api-overview/resources/arm/ for more details.

let parseCLI (key:string) (argv: string[]) =
    argv
    |> Array.tryFind (fun x -> x.ToLower().StartsWith(key.ToLower()))
    |> Option.bind (fun x -> x.Split('=', StringSplitOptions.RemoveEmptyEntries) |> Array.tryItem 1)
    
let outputPath = "./output"

let rec copyFilesRecursively (source: DirectoryInfo) (target: DirectoryInfo) =
    source.GetDirectories()
    |> Seq.iter (fun dir -> copyFilesRecursively dir (target.CreateSubdirectory dir.Name))
    source.GetFiles()
    |> Seq.iter (fun file -> file.CopyTo(target.FullName + "/" + file.Name, true) |> ignore)

[<EntryPoint>]
let main argv =
    let (storageName, webAppName, rgName) =
        match parseCLI "deployEnvironment" argv with
        | Some "test" -> "activegamenighttest", "active-game-night-test", "activegamenight-test"
        | Some "prod" -> "activegamenight", "active-game-night", "activegamenight"
        | _ -> failwith "invalid deployEnvironment"
        
    let azureAppId = Environment.GetEnvironmentVariable("AGN_AZURE_APPID")
    let azureSecret = Environment.GetEnvironmentVariable("AGN_AZURE_SECRET")
    let azureTenant = Environment.GetEnvironmentVariable("AGN_AZURE_TENANT")

    let deployment =
        let storage = storageAccount {
            name storageName
            sku Storage.Standard_LRS
        }
    
        let webApp = webApp {
            name webAppName
            operating_system Windows
            runtime_stack Runtime.DotNetCore31
            https_only
            always_on
            sku WebApp.Sku.B1
            setting "AzureStorageConnectionString" storage.Key
            setting "ServerPort" "8080"
            setting "PublicPath" "./public"
            setting "BasePath" (sprintf "https://%s.azurewebsites.net" webAppName) 
            zip_deploy outputPath
        }
        
        arm {
            location Location.WestEurope
            add_resources [
                storage
                webApp
            ]
        }

    printfn "Copy public folder to %s" outputPath
    let source = DirectoryInfo("./src/Backend/public")
    let destination = DirectoryInfo("./output/public")
    destination.Create()
    copyFilesRecursively source destination
    
    Deploy.authenticate azureAppId azureSecret azureTenant
    |> printfn "%A"
    
    deployment 
    |> Deploy.execute ("ActiveGameNight" + rgName) Deploy.NoParameters
    |> printfn "%A"
    
    0