module Backend.Configuration

open FsConfig
open Microsoft.Extensions.Configuration
open System.IO
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
        .AddUserSecrets("875c48eb-8be3-4f1f-9ba0-02274d8c9d58")
        // .AddEnvironmentVariables()
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
            {| ConnectionString = Storage.ConnectionString config.AzureStorageConnectionString |}
        | Error msg ->
            match msg with
            | BadValue (name, msg) -> invalidArg name msg
            | ConfigParseError.NotFound name -> invalidArg name "Not found"
            | NotSupported name -> invalidArg name "Could not read config value"

