[<RequireQualifiedAccess>]
module Storage

open Infrastructure
open FSharp.Azure.Storage.Table
open Domain
open FsToolkit.ErrorHandling
open FSharpPlus.Data
open Microsoft.Azure.Cosmos.Table


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
    
type ITables =
    abstract InGameNightTable : Operation<GameNightEntity> -> Async<OperationResult>
    abstract FromGameNightTable : EntityQuery<GameNightEntity> -> Async<seq<GameNightEntity * EntityMetadata>>
    
type IStorage =
    abstract Tables: ITables

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
          
        
open Implementation
        
let saveConfirmedGameNight (env: #IStorage) (gn: ConfirmedGameNight) : Async<unit> =
    let dateVotes = (gn.Date, gn.Players |> NonEmptySet.toSet) |> List.singleton |> NonEmptyMap.ofList  
    { PartitionKey = partitionKey
      Id = gn.Id |> GameNightId.toString
      GameVotes = serializeGameVotes gn.GameVotes
      DateVotes = serializeDateVotes dateVotes
      CreatedBy = gn.CreatedBy.Val
      Status = confirmedStatus }
    |> InsertOrMerge
    |> env.Tables.InGameNightTable
    |> Async.map ensureSuccessful
    
let saveCancelledGameNight (env: #IStorage) (gn: CancelledGameNight) : Async<unit> =
    { PartitionKey = partitionKey
      Id = gn.Id |> GameNightId.toString
      GameVotes = serializeGameVotes gn.GameVotes
      DateVotes = serializeDateVotes gn.DateVotes
      CreatedBy = gn.CreatedBy.Val
      Status = cancelledStatus }
    |> InsertOrMerge
    |> env.Tables.InGameNightTable
    |> Async.map ensureSuccessful
    
let getProposedGameNight (env: #IStorage) id : AsyncResult<ProposedGameNight, NotFoundError> =
    let stringId = GameNightId.toString id
    asyncResult {
        try
            let! result =
                Query.all<GameNightEntity>
                |> Query.where <@ fun _ s -> s.PartitionKey = partitionKey && s.RowKey = stringId @>
                |> env.Tables.FromGameNightTable
            return 
                result 
                |> Seq.head 
                |> fst 
                |> toProposedGameNight
        with _ -> return! Error NotFoundError // TODO check status code
    }
    
let getAllProposedGameNights (env: #IStorage) : Async<ProposedGameNight list> =
    async {
        let! result =
            Query.all<GameNightEntity>
            |> Query.where <@ fun g _ -> g.Status = proposedStatus @>
            |> env.Tables.FromGameNightTable
        return
            result
            |> Seq.map (fst >> toProposedGameNight)
            |> Seq.sortBy (fun x -> x.DateVotes |> NonEmptyMap.toList |> List.minBy (fun (date,_) -> date))
            |> List.ofSeq
    }
    
let getAllConfirmedGameNights (env: #IStorage) : Async<ConfirmedGameNight list> =
    async {
        let! result =
            Query.all<GameNightEntity>
            |> Query.where <@ fun g _ -> g.Status = confirmedStatus @>
            |> env.Tables.FromGameNightTable
        return
            result
            |> Seq.map (fst >> toConfirmedGameNight)
            |> Seq.sortBy (fun x -> x.Date)
            |> List.ofSeq
    }
    
let getAllCancelledGameNights (env: #IStorage) : Async<CancelledGameNight list> =
    async {
        let! result =
            Query.all<GameNightEntity>
            |> Query.where <@ fun g _ -> g.Status = proposedStatus @>
            |> env.Tables.FromGameNightTable
        return
            result
            |> Seq.map (fst >> toCancelledGameNight)
            |> Seq.sortBy (fun x -> x.DateVotes |> NonEmptyMap.toList |> List.minBy (fun (date,_) -> date))
            |> List.ofSeq
    }
    
let saveProposedGameNight (env: #IStorage) (gn: ProposedGameNight) : Async<unit> =
    { PartitionKey = partitionKey
      Id = gn.Id |> GameNightId.toString
      GameVotes = serializeGameVotes gn.GameVotes
      DateVotes = serializeDateVotes gn.DateVotes
      CreatedBy = gn.CreatedBy.Val
      Status = proposedStatus }
    |> InsertOrMerge
    |> env.Tables.InGameNightTable
    |> Async.map ensureSuccessful
    
let live (ConnectionString connectionString) : ITables =
    
    let account = CloudStorageAccount.Parse connectionString
    let tableClient = account.CreateCloudTableClient()    
    
    { new ITables with
        member _.InGameNightTable(operation) = inTableAsync tableClient gameNightsTable operation
        member _.FromGameNightTable(query) = fromTableAsync tableClient gameNightsTable query }