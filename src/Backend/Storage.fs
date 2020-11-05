module Backend.Storage

open FSharp.Azure.Storage.Table
open Microsoft.Azure.Cosmos.Table
open Backend
open Backend.Extensions
open FsToolkit.ErrorHandling

type GameEntity = 
    { [<PartitionKey>] PartitionKey : string
      [<RowKey>] Id : string
      Name : string
      NumberOfPlayers : int option
      Link : string
      Notes : string
    }

type ProposedGameNightEntity = 
    { [<PartitionKey>] PartitionKey : string
      [<RowKey>] Id : string
      Games : string
      Dates : string
      ProposedBy : string }
    
type ConnectionString = ConnectionString of string
with member this.Val = this |> fun (ConnectionString cs) -> cs
    
let proposedGameNightsTable = "ProposedGameNights"
let partitionKey = "AGN"
    
module Implementation =
    let serializeGamesMap (gs: Map<GameName, Set<User>>) =
        gs
        |> Map.toList
        |> List.map (fun (x, y) -> (x, y |> Set.toList))
        |> Json.serialize
        
    let deserializeGamesMap (str: string) : Map<GameName, Set<User>> =
        Json.deserialize str
        |> Map.ofList
        
    let serializeDatesMap (ds: Map<Date, Set<User>>) =
        let result =
            ds
            |> Map.toList
            |> List.map (fun (k,v) -> (k |> Date.toDateTime, v |> Set.toList))
            |> Json.serialize
        result
        
    let deserializeDatesMap (str: string) : Map<Date, Set<User>> =
        Json.deserialize str
        |> List.map (fun (k,v) -> 
            Date.tryParse k |> Result.valueOr (fun (ValidationError d) -> failwithf "date deserialization failed %s" d), v)
        |> Map.ofList
        
    let saveProposedGameNight inGameNightTable (gn: ProposedGameNight) : Async<OperationResult> =
        { PartitionKey = partitionKey
          Id = gn.Id |> GameNightId.toString
          Games = serializeGamesMap gn.Games
          Dates = serializeDatesMap gn.Dates
          ProposedBy = gn.ProposedBy.Val }
        |> InsertOrMerge
        |> inGameNightTable
        
    let toProposedGameNight (entity: ProposedGameNightEntity) =
        { ProposedGameNight.Id = GameNightId.parse entity.Id |> Result.orFail
          Games = deserializeGamesMap entity.Games
          Dates = deserializeDatesMap entity.Dates
          ProposedBy = User.create entity.ProposedBy |> Result.orFail }
          
    let getProposedGameNight (fromGameNightTable : EntityQuery<ProposedGameNightEntity> -> Async<seq<ProposedGameNightEntity * EntityMetadata>>) id : AsyncResult<ProposedGameNight, NotFoundError> =
        let stringId = GameNightId.toString id
        asyncResult {
            try
                let! result =
                    Query.all<ProposedGameNightEntity>
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
                Query.all<ProposedGameNightEntity>
                |> fromGameNightTable
            return
                result
                |> Seq.map (fst >> toProposedGameNight)
                |> Seq.sortBy (fun x -> x.Dates |> Map.toList |> List.minBy (fun (date,_) -> date))
        }

open Implementation

type Service(connectionString : ConnectionString) =
    let account = CloudStorageAccount.Parse connectionString.Val
    let tableClient = account.CreateCloudTableClient()
    
    do (tableClient.GetTableReference proposedGameNightsTable).CreateIfNotExists() |> ignore
    
    let inGameNightTable = inTableAsync tableClient proposedGameNightsTable
    let fromGameNightTable = fromTableAsync tableClient proposedGameNightsTable
    
    member _.SaveGame game = ()
    member _.GetGame id = ()
    member _.SaveProposedGameNight (gameNight: ProposedGameNight) = saveProposedGameNight inGameNightTable gameNight
    member _.GetProposedGameNights () = getAllProposedGameNights fromGameNightTable ()
    member _.GetProposedGameNight (id) = getProposedGameNight fromGameNightTable id

