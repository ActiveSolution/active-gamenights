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
    let deployEnvironment = Environment.GetEnvironmentVariable("AGN_ENVIRONMENT")
    let azureAppId = Environment.GetEnvironmentVariable("AGN_AZURE_APPID")
    let azureSecret = Environment.GetEnvironmentVariable("AGN_AZURE_SECRET")
    let azureTenant = Environment.GetEnvironmentVariable("AGN_AZURE_TENANT")

    let deployment env =
        let storage = storageAccount {
            name ("activegamenight" + env)
            sku Storage.Standard_LRS
        }
    
        let webAppEnv = "-" + env
        let webAppName = "active-game-night" + webAppEnv
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
    printf "Generating ARM template..."
    deployment |> Writer.quickWrite "output"
    printfn "all done! Template written to output.json"

    // Alternatively, deploy your resource group directly to Azure here.

    let webAppEnv =
        match deployEnvironment with
        | "test" -> "-test"
        | "prod" -> ""
        | _ -> failwith "invalid deployEnvironment"
    Deploy.authenticate azureAppId azureSecret azureTenant
    |> printfn "%A"
    
    deployment 
    |> Deploy.execute ("activegamenight" + webAppEnv) Deploy.NoParameters
    |> printfn "%A"
    
    0