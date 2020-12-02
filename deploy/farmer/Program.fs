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
    
let webAppOutput = "./output/webApp"
let functionsOutput = "./output/functions"

[<EntryPoint>]
let main argv =
    let (storageName, webAppName, rgName, domain) =
        match parseCLI "deployEnvironment" argv with
        | Some "test" -> "activegamenighttest", "active-game-night-test", "activegamenight-test", "active-game-night-test.azurewebsites.net"
        | Some "prod" -> "activegamenight", "active-game-night", "activegamenight", "active-game-night.azurewebsites.net"
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
            operating_system Linux
            runtime_stack Runtime.DotNetCore31
            https_only
            always_on
            sku WebApp.Sku.B1
            setting "AzureStorageConnectionString" storage.Key
            setting "ServerPort" "8080"
            setting "PublicPath" "./public"
            setting "BasePath" (sprintf "https://%s.azurewebsites.net" webAppName) 
            setting "Domain" domain
            zip_deploy webAppOutput
        }

        let functions = functions {
            name (webAppName + "-functions")
            link_to_storage_account storage.Name.ResourceName
            operating_system Linux
            link_to_service_plan webApp.ServicePlanName
            zip_deploy functionsOutput
            setting "ENABLE_ORYX_BUILD" "false" // az cli zip-deploy for linux az functions consumption plan. tworkaround from https://github.com/Azure/Azure-Functions/issues/1200
        }
        
        arm {
            location Location.WestEurope
            add_resources [
                storage
                webApp
                functions
            ]
        }
    
    Deploy.authenticate azureAppId azureSecret azureTenant
    |> printfn "%A"
    
    deployment 
    |> Deploy.execute rgName Deploy.NoParameters
    |> printfn "%A"
    
    0