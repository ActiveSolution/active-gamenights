module Storage

open Infrastructure
open FSharp.Azure.Storage.Table
open Microsoft.Azure.Cosmos.Table
open Domain
open FsToolkit.ErrorHandling
open FSharpPlus.Data


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
    let ensureSuccessful (o: OperationResult) =
        if o.HttpStatusCode >= 200 && o.HttpStatusCode < 300 then () else failwithf "Table storage operation not successful %i" o.HttpStatusCode
    
    let okOrFail msg = Result.defaultWith (fun _ -> failwith msg)
    let someOrFail msg = Option.defaultWith (fun _ -> failwith msg)
    let parseUser entityUser = User.create entityUser |> okOrFail "Entity has invalid CreatedBy"
    let parseGameNightId entityId =
        GameNightId.parse entityId |> okOrFail "Entity has invalid GameNightId"
    
    [<AutoOpen>]
    module Helpers =
        let serializeGameVotes (gs: NonEmptyMap<GameName, Set<User>>) =
            gs
            |> NonEmptyMap.toList
            |> List.map (fun (x, y) -> (x, y |> Set.toList))
            |> Json.serialize
            
        let deserializeGameVotes (str: string) : NonEmptyMap<GameName, Set<User>> =
            Json.deserialize str
            |> NonEmptyMap.ofList
            
        let serializeDateVotes (ds: NonEmptyMap<Date, Set<User>>) =
            let result =
                ds
                |> NonEmptyMap.toList
                |> List.map (fun (k,v) -> (k |> Date.toDateTime, v |> Set.toList))
                |> Json.serialize
            result
            
        let deserializeDateVotes (str: string) : NonEmptyMap<Date, Set<User>> =
            Json.deserialize str
            |> List.map (fun (k,v) -> 
                Date.tryParse k |> Result.valueOr (fun (ValidationError d) -> failwithf "date deserialization failed %s" d), v)
            |> NonEmptyMap.ofList
        
    let saveProposedGameNight inGameNightTable (gn: ProposedGameNight) : Async<unit> =
        { PartitionKey = partitionKey
          Id = gn.Id |> GameNightId.toString
          GameVotes = serializeGameVotes gn.GameVotes
          DateVotes = serializeDateVotes gn.DateVotes
          CreatedBy = gn.CreatedBy.Val
          Status = proposedStatus }
        |> InsertOrMerge
        |> inGameNightTable
        |> Async.map ensureSuccessful
        
    let saveConfirmedGameNight inGameNightTable (gn: ConfirmedGameNight) : Async<unit> =
        let dateVotes = (gn.Date, gn.Players |> NonEmptySet.toSet) |> List.singleton |> NonEmptyMap.ofList  
        { PartitionKey = partitionKey
          Id = gn.Id |> GameNightId.toString
          GameVotes = serializeGameVotes gn.GameVotes
          DateVotes = serializeDateVotes dateVotes
          CreatedBy = gn.CreatedBy.Val
          Status = confirmedStatus }
        |> InsertOrMerge
        |> inGameNightTable
        |> Async.map ensureSuccessful
        
    let saveCancelledGameNight inGameNightTable (gn: CancelledGameNight) : Async<unit> =
        { PartitionKey = partitionKey
          Id = gn.Id |> GameNightId.toString
          GameVotes = serializeGameVotes gn.GameVotes
          DateVotes = serializeDateVotes gn.DateVotes
          CreatedBy = gn.CreatedBy.Val
          Status = cancelledStatus }
        |> InsertOrMerge
        |> inGameNightTable
        |> Async.map ensureSuccessful

        
    let toProposedGameNight (entity: GameNightEntity) =
        { ProposedGameNight.Id = parseGameNightId entity.Id
          GameVotes = deserializeGameVotes entity.GameVotes
          DateVotes = deserializeDateVotes entity.DateVotes
          CreatedBy = parseUser entity.CreatedBy}
    
    let toConfirmedGameNight (entity: GameNightEntity) =
        let date, players =
            entity.DateVotes
            |> deserializeDateVotes
            |> NonEmptyMap.toList
            |> List.tryExactlyOne
            |> someOrFail "Confirmed Game Night should have exactly one date"
           
        { ConfirmedGameNight.Id = parseGameNightId entity.Id
          GameVotes = deserializeGameVotes entity.GameVotes
          Date = date
          Players = players |> NonEmptySet.ofSet
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
            with _ -> return! Error NotFoundError // TODO check status code
        }
        
    let getAllProposedGameNights fromGameNightTable () : Async<ProposedGameNight list> =
        async {
            let! result =
                Query.all<GameNightEntity>
                |> Query.where <@ fun g _ -> g.Status = proposedStatus @>
                |> fromGameNightTable
            return
                result
                |> Seq.map (fst >> toProposedGameNight)
                |> Seq.sortBy (fun x -> x.DateVotes |> NonEmptyMap.toList |> List.minBy (fun (date,_) -> date))
                |> List.ofSeq
        }
        
    let getAllConfirmedGameNights fromGameNightTable () : Async<ConfirmedGameNight list> =
        async {
            let! result =
                Query.all<GameNightEntity>
                |> Query.where <@ fun g _ -> g.Status = confirmedStatus @>
                |> fromGameNightTable
            return
                result
                |> Seq.map (fst >> toConfirmedGameNight)
                |> Seq.sortBy (fun x -> x.Date)
                |> List.ofSeq
        }
        
    let getAllCancelledGameNights fromGameNightTable () : Async<CancelledGameNight list> =
        async {
            let! result =
                Query.all<GameNightEntity>
                |> Query.where <@ fun g _ -> g.Status = proposedStatus @>
                |> fromGameNightTable
            return
                result
                |> Seq.map (fst >> toCancelledGameNight)
                |> Seq.sortBy (fun x -> x.DateVotes |> NonEmptyMap.toList |> List.minBy (fun (date,_) -> date))
                |> List.ofSeq
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

