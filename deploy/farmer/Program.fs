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

[<EntryPoint>]
let main argv =
    let deployEnvironment = 
        match parseCLI "deployEnvironment" argv with
        | Some "test" -> "test"
        | Some "prod" -> ""
        | _ -> failwith "invalid deployEnvironment"
        
    let azureAppId = Environment.GetEnvironmentVariable("AGN_AZURE_APPID")
    let azureSecret = Environment.GetEnvironmentVariable("AGN_AZURE_SECRET")
    let azureTenant = Environment.GetEnvironmentVariable("AGN_AZURE_TENANT")

    let deployment env =
        let storage = storageAccount {
            name ("activegamenight" + env)
            sku Storage.Standard_LRS
        }
    
        let webAppName = "active-game-night-" + deployEnvironment
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
            zip_deploy "./output"
        }
        
        arm {
            location Location.WestEurope
            add_resources [
                storage
                webApp
            ]
        }

    let deployment = deployment deployEnvironment
    
    Deploy.authenticate azureAppId azureSecret azureTenant
    |> printfn "%A"
    
    let rgName =
        match deployEnvironment with
        | "test" -> "-test"
        | "" -> ""
        | _ -> failwith "Invalid deploy enviroment"
    deployment 
    |> Deploy.execute rgName Deploy.NoParameters
    |> printfn "%A"
    
    0