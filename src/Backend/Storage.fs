module Backend.Storage

open FSharp.Azure.Storage.Table
open Microsoft.Azure.Cosmos.Table
open Backend
open FsToolkit.ErrorHandling

type GameEntity = 
    { [<PartitionKey>] PartitionKey : string
      [<RowKey>] Id : string
      Name : string
      NumberOfPlayers : int option
      Link : string
      Notes : string
    }

type GameNightEntity = 
    { [<PartitionKey>] PartitionKey : string
      [<RowKey>] Id : string
      GameVotes : string
      DateVotes : string
      CreatedBy : string
      Status : string }
    
type ConnectionString = ConnectionString of string
    with member this.Val = this |> fun (ConnectionString cs) -> cs
    

    
module Implementation =
    let gameNightsTable = "GameNights"
    let partitionKey = "AGN"
    let proposedStatus = "proposed"
    let confirmedStatus = "confirmed"
    let completedStatus = "completed"
    let cancelledStatus = "cancelled"
    
    let okOrFail msg = Result.defaultWith (fun _ -> failwith msg)
    let someOrFail msg = Option.defaultWith (fun _ -> failwith msg)
    let parseUser entityUser = User.create entityUser |> okOrFail "Entity has invalid CreatedBy"
    let parseGameNightId entityId =
        printfn "entityId %A" entityId
        GameNightId.parse entityId |> okOrFail "Entity has invalid GameNightId"
    
    [<AutoOpen>]
    module Helpers =
        let serializeGameVotes (gs: Map<GameName, Set<User>>) =
            gs
            |> Map.toList
            |> List.map (fun (x, y) -> (x, y |> Set.toList))
            |> Json.serialize
            
        let deserializeGameVotes (str: string) : Map<GameName, Set<User>> =
            Json.deserialize str
            |> Map.ofList
            
        let serializeDateVotes (ds: Map<Date, Set<User>>) =
            let result =
                ds
                |> Map.toList
                |> List.map (fun (k,v) -> (k |> Date.toDateTime, v |> Set.toList))
                |> Json.serialize
            result
            
        let deserializeDateVotes (str: string) : Map<Date, Set<User>> =
            Json.deserialize str
            |> List.map (fun (k,v) -> 
                Date.tryParse k |> Result.valueOr (fun (ValidationError d) -> failwithf "date deserialization failed %s" d), v)
            |> Map.ofList
        
    let saveProposedGameNight inGameNightTable (gn: ProposedGameNight) : Async<OperationResult> =
        { PartitionKey = partitionKey
          Id = gn.Id |> GameNightId.toString
          GameVotes = serializeGameVotes gn.GameVotes
          DateVotes = serializeDateVotes gn.DateVotes
          CreatedBy = gn.CreatedBy.Val
          Status = proposedStatus }
        |> InsertOrMerge
        |> inGameNightTable
        
    let saveConfirmedGameNight inGameNightTable (gn: ConfirmedGameNight) : Async<OperationResult> =
        let dateVotes = (gn.Date, gn.Players) |> List.singleton |> Map.ofList  
        { PartitionKey = partitionKey
          Id = gn.Id |> GameNightId.toString
          GameVotes = serializeGameVotes gn.GameVotes
          DateVotes = serializeDateVotes dateVotes
          CreatedBy = gn.CreatedBy.Val
          Status = confirmedStatus }
        |> InsertOrMerge
        |> inGameNightTable
        
    let saveCancelledGameNight inGameNightTable (gn: CancelledGameNight) : Async<OperationResult> =
        { PartitionKey = partitionKey
          Id = gn.Id |> GameNightId.toString
          GameVotes = serializeGameVotes gn.GameVotes
          DateVotes = serializeDateVotes gn.DateVotes
          CreatedBy = gn.CreatedBy.Val
          Status = cancelledStatus }
        |> InsertOrMerge
        |> inGameNightTable
        
    let toProposedGameNight (entity: GameNightEntity) =
        { ProposedGameNight.Id = parseGameNightId entity.Id
          GameVotes = deserializeGameVotes entity.GameVotes
          DateVotes = deserializeDateVotes entity.DateVotes
          CreatedBy = parseUser entity.CreatedBy}
    
    let toConfirmedGameNight (entity: GameNightEntity) =
        let date, players =
            entity.DateVotes
            |> deserializeDateVotes
            |> Map.toList
            |> List.tryExactlyOne
            |> someOrFail "Confirmed Game Night should have exactly one date"
           
        { ConfirmedGameNight.Id = parseGameNightId entity.Id
          GameVotes = deserializeGameVotes entity.GameVotes
          Date = date
          Players = players
          CreatedBy = parseUser entity.CreatedBy }
          
    let toCancelledGameNight (entity: GameNightEntity) =
        { CancelledGameNight.Id = parseGameNightId entity.Id
          GameVotes = deserializeGameVotes entity.GameVotes
          DateVotes = deserializeDateVotes entity.DateVotes
          CreatedBy = parseUser entity.CreatedBy}
          
    let getProposedGameNight (fromGameNightTable : EntityQuery<GameNightEntity> -> Async<seq<GameNightEntity * EntityMetadata>>) id : AsyncResult<ProposedGameNight, NotFoundError> =
        let stringId = GameNightId.toString id
        asyncResult {
            try
                let! result =
                    Query.all<GameNightEntity>
                    |> Query.where <@ fun _ s -> s.PartitionKey = partitionKey && s.RowKey = stringId @>
                    |> fromGameNightTable
                return 
                    result 
                    |> Seq.head 
                    |> fst 
                    |> toProposedGameNight
            with exn -> return! Error NotFoundError
        }
        
    let getAllProposedGameNights fromGameNightTable () : Async<ProposedGameNight seq> =
        async {
            let! result =
                Query.all<GameNightEntity>
                |> Query.where <@ fun g _ -> g.Status = proposedStatus @>
                |> fromGameNightTable
            return
                result
                |> Seq.map (fst >> toProposedGameNight)
                |> Seq.sortBy (fun x -> x.DateVotes |> Map.toList |> List.minBy (fun (date,_) -> date))
        }
        
    let getAllConfirmedGameNights fromGameNightTable () : Async<ConfirmedGameNight seq> =
        async {
            let! result =
                Query.all<GameNightEntity>
                |> Query.where <@ fun g _ -> g.Status = confirmedStatus @>
                |> fromGameNightTable
            return
                result
                |> Seq.map (fst >> toConfirmedGameNight)
                |> Seq.sortBy (fun x -> x.Date)
        }
        
    let getAllCancelledGameNights fromGameNightTable () : Async<CancelledGameNight seq> =
        async {
            let! result =
                Query.all<GameNightEntity>
                |> Query.where <@ fun g _ -> g.Status = proposedStatus @>
                |> fromGameNightTable
            return
                result
                |> Seq.map (fst >> toCancelledGameNight)
                |> Seq.sortBy (fun x -> x.DateVotes |> Map.toList |> List.minBy (fun (date,_) -> date))
        }
        
open Implementation         

type Service(connectionString : ConnectionString) =
    let account = CloudStorageAccount.Parse connectionString.Val
    let tableClient = account.CreateCloudTableClient()
    
    do (tableClient.GetTableReference gameNightsTable).CreateIfNotExists() |> ignore
    
    let inGameNightTable = inTableAsync tableClient gameNightsTable
    let fromGameNightTable = fromTableAsync tableClient gameNightsTable
    
    member _.SaveProposedGameNight (gameNight: ProposedGameNight) = saveProposedGameNight inGameNightTable gameNight
    member _.GetProposedGameNights () = getAllProposedGameNights fromGameNightTable ()
    member _.GetProposedGameNight (id) = getProposedGameNight fromGameNightTable id
    member _.SaveConfirmedGameNight (gameNight: ConfirmedGameNight) = saveConfirmedGameNight inGameNightTable gameNight
    member _.GetConfirmedGameNights () = getAllConfirmedGameNights fromGameNightTable ()
    member _.SaveCancelledGameNight (gameNight: CancelledGameNight) = saveCancelledGameNight inGameNightTable gameNight
    member _.GetCancelledGameNights () = getAllCancelledGameNights fromGameNightTable ()

