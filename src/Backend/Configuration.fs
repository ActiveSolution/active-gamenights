module Backend.Configuration

open FsConfig
open Microsoft.Extensions.Configuration
open System.IO
open Backend
open Common


type Config =
    { [<DefaultValue("UseDevelopmentStorage=true")>]
      AzureStorageConnectionString: string
      [<DefaultValue("8085")>]
      ServerPort: uint16
      [<DefaultValue("../../output/server/public")>]
      PublicPath: string
      [<DefaultValue("https://localhost:8085/")>]
      BasePath: string 
      [<DefaultValue("localhost")>]
      Domain: string }
    
let configRoot =
    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddUserSecrets("53976580-fec0-49dd-8e4a-64509d07c664")
        .AddEnvironmentVariables()
        .Build()

let [<Literal>] ConfigErrorMsg =
    """


************************************************************
* Failed to read configuration variables.
* For local development you need to add local user-secrets,
************************************************************


"""

let config =
    lazy
        let appConfig = AppConfig(configRoot)

        match appConfig.Get<Config>() with
        | Ok config ->
            {| ConnectionString = Storage.ConnectionString config.AzureStorageConnectionString
               ServerPort = config.ServerPort
               PublicPath = config.PublicPath |> Path.GetFullPath
               BasePath = BasePath config.BasePath
               Domain = Domain config.Domain |}
        | Error msg ->
            match msg with
            | BadValue (name, msg) -> invalidArg name msg
            | ConfigParseError.NotFound name -> invalidArg name "Not found"
            | NotSupported name -> invalidArg name "Could not read config value"

