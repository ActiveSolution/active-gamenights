namespace Functions.Functions

open System
open System.Threading.Tasks
open Common
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host
open Microsoft.Extensions.Logging
open Domain
open FsToolkit.ErrorHandling
open Functions
open Domain.Date.Operators
open FSharpPlus.Data
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http



module Functions =
    
    module Implementation =
        let isDueForConfirmation dueDate (gn: ProposedGameNight) =
            let earliestDate =
                gn.DateVotes
                |> NonEmptyMap.keys
                |> Seq.min
                
            earliestDate <= dueDate
                
        let confirmGameNights (storage: Storage.Service) =
            let confirmGameNight gn =
                match Workflows.GameNights.confirmGameNight gn with
                | Workflows.GameNights.Confirmed gn ->
                    storage.SaveConfirmedGameNight gn
                | Workflows.GameNights.Cancelled gn ->
                    storage.SaveCancelledGameNight gn
        
            async {
                let dueDate = Date.today() + TimeSpan.FromDays(2.)
                let! gameNights = storage.GetProposedGameNights()
                return!
                    gameNights
                    |> List.filter (isDueForConfirmation dueDate)
                    |> List.map confirmGameNight
                    |> Async.Sequential
                    |> Async.map (ignore)
            }
            
    open Implementation
    
    [<FunctionName("ConfirmGameNight")>]
    let runConfirmGameNight([<TimerTrigger("*/5 * * * *")>]myTimer: TimerInfo, log: ILogger) =
        let msg = sprintf "ConfirmGameNight function executed at: %A" DateTime.Now
        log.LogInformation msg
        
        confirmGameNights CompositionRoot.storage
        |> Async.StartAsTask :> Task
        
    [<FunctionName("Status")>]
    let runStatus([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status")>](req: HttpRequest), log: ILogger) =
        let msg = sprintf "Status function executed at: %A" DateTime.Now
        log.LogInformation msg
        
        sprintf "ActiveGameNight functions ok at %s" (DateTime.Now.ToString()) |> OkObjectResult
        