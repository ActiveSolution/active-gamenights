[<RequireQualifiedAccess>]
module Storage

open Infrastructure
open FSharp.Azure.Storage.Table
open Domain
open FsToolkit.ErrorHandling
open FSharpPlus.Data
open Microsoft.Azure.Cosmos.Table
open FSharp.UMX
open System


type GameEntity = 
    { [<PartitionKey>] PartitionKey : string
      [<RowKey>] Id : string
      Name : string
      NumberOfPlayers : string
      Link : string
      Notes : string
      CreatedBy : string
      ImageUrl : string
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
    abstract InGameTable : Operation<GameEntity> -> Async<OperationResult>
    abstract FromGameTable : EntityQuery<GameEntity> -> Async<seq<GameEntity * EntityMetadata>>
    
type IStorage =
    abstract Tables: ITables


[<AutoOpen>]
module private Implementation =
    let gameNightsTable = "GameNights"
    let gamesTable = "Games"
    let partitionKey = "AGN"
    let proposedStatus = "proposed"
    let confirmedStatus = "confirmed"
    let completedStatus = "completed"
    let cancelledStatus = "cancelled"
    let ensureSuccessful (o: OperationResult) =
        if o.HttpStatusCode >= 200 && o.HttpStatusCode < 300 then () else failwithf "Table storage operation not successful %i" o.HttpStatusCode
    
    let okOrFail msg = Result.defaultWith (fun _ -> failwith msg)
    let someOrFail msg = Option.defaultWith (fun _ -> failwith msg)
    let parseUser entityUser = Username.create entityUser |> okOrFail "Entity has invalid CreatedBy"
    

module GameNights =
    let private parseGameNightId entityId =
        GameNightId.parse entityId |> okOrFail "Entity has invalid GameNightId"

    let private serializeGameVotes (gs: NonEmptyMap<string<CanonizedGameName>, Set<string<CanonizedUsername>>>) =
        gs
        |> NonEmptyMap.toList
        |> List.map (fun (x, y) -> (x, y |> Set.toList))
        |> Json.serialize
        
    let private deserializeGameVotes (str: string) : NonEmptyMap<string<CanonizedGameName>, Set<string<CanonizedUsername>>> =
        Json.deserialize str
        |> NonEmptyMap.ofList
        
    let private serializeDateVotes (ds: NonEmptyMap<DateTime, Set<string<CanonizedUsername>>>) =
        let result =
            ds
            |> NonEmptyMap.toList
            |> List.map (fun (k,v) -> (k, v |> Set.toList))
            |> Json.serialize
        result
        
    let private deserializeDateVotes (str: string) : NonEmptyMap<DateTime, Set<string<CanonizedUsername>>> =
        Json.deserialize str
        |> List.map (fun (k, votes) -> 
            DateTime.tryParse k |> Result.valueOr (failwithf "date deserialization failed %s"), votes)
        |> NonEmptyMap.ofList
    
    let private toProposedGameNight (entity: GameNightEntity) =
        { ProposedGameNight.Id = parseGameNightId entity.Id
          GameVotes = deserializeGameVotes entity.GameVotes
          DateVotes = deserializeDateVotes entity.DateVotes
          CreatedBy = parseUser entity.CreatedBy}
    
    let private toConfirmedGameNight (entity: GameNightEntity) =
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
          
    let private toCancelledGameNight (entity: GameNightEntity) =
        { CancelledGameNight.Id = parseGameNightId entity.Id
          GameVotes = deserializeGameVotes entity.GameVotes
          DateVotes = deserializeDateVotes entity.DateVotes
          CreatedBy = parseUser entity.CreatedBy}
        
    let saveConfirmedGameNight (env: #IStorage) (gn: ConfirmedGameNight) : Async<unit> =
        let dateVotes = (gn.Date, gn.Players |> NonEmptySet.toSet) |> List.singleton |> NonEmptyMap.ofList  
        { PartitionKey = partitionKey
          Id = gn.Id.ToString()
          GameVotes = serializeGameVotes gn.GameVotes
          DateVotes = serializeDateVotes dateVotes
          CreatedBy = %gn.CreatedBy
          Status = confirmedStatus }
        |> InsertOrReplace
        |> env.Tables.InGameNightTable
        |> Async.map ensureSuccessful
        
    let saveCancelledGameNight (env: #IStorage) (gn: CancelledGameNight) : Async<unit> =
        { PartitionKey = partitionKey
          Id = gn.Id.ToString()
          GameVotes = serializeGameVotes gn.GameVotes
          DateVotes = serializeDateVotes gn.DateVotes
          CreatedBy = %gn.CreatedBy
          Status = cancelledStatus }
        |> InsertOrReplace
        |> env.Tables.InGameNightTable
        |> Async.map ensureSuccessful
        
    let getProposedGameNight (env: #IStorage) (id: Guid<GameNightId>) : AsyncResult<ProposedGameNight, string> =
        let stringId = id.ToString()
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
            with _ -> return! Error "not found" // TODO check status code
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
          Id = gn.Id.ToString()
          GameVotes = serializeGameVotes gn.GameVotes
          DateVotes = serializeDateVotes gn.DateVotes
          CreatedBy = %gn.CreatedBy
          Status = proposedStatus }
        |> InsertOrReplace
        |> env.Tables.InGameNightTable
        |> Async.map ensureSuccessful

module Games =
    let private toGame (entity: GameEntity) =
        { Game.CreatedBy = entity.CreatedBy |> Username.create |> okOrFail "Invalid username in db"
          Game.ImageUrl = entity.ImageUrl |> Option.ofObj
          Game.Link = entity.Link |> Option.ofObj
          Game.Name = GameName.create entity.Name |> okOrFail "Invalid game name in db"
          Game.Notes = entity.Notes |> Option.ofObj
          Game.NumberOfPlayers = entity.NumberOfPlayers |> Option.ofObj }

    let getAllGames (env: #IStorage) : Async<Set<Game>> =
        async {
            let! result =
                Query.all<GameEntity>
                |> env.Tables.FromGameTable
            return
                result
                |> Seq.map (fst >> toGame)
                |> Seq.sortBy (fun x -> x.Name)
                |> Set.ofSeq
        }

    let getGame (env: #IStorage) (gameName: string<CanonizedGameName>) : Async<Game> =
        async {
            let! result =
                let gameNameStr = %gameName
                Query.all<GameEntity>
                |> Query.where <@ fun _ s -> s.PartitionKey = partitionKey && s.RowKey = gameNameStr @>
                |> env.Tables.FromGameTable
            return 
                result 
                |> Seq.head 
                |> fst 
                |> toGame
        }

    let addGame (env: #IStorage) (game: Game) : Async<unit> = 
        { GameEntity.PartitionKey = partitionKey
          Id = game.Name.ToString()
          Name = game.Name.ToString()
          NumberOfPlayers = game.NumberOfPlayers |> Option.toObj
          Link = game.Link |> Option.toObj
          ImageUrl = game.ImageUrl |> Option.toObj 
          Notes = game.Notes |> Option.toObj
          CreatedBy = %game.CreatedBy }
        |> InsertOrReplace
        |> env.Tables.InGameTable
        |> Async.map ensureSuccessful

let live (ConnectionString connectionString) : ITables =
    
    let account = CloudStorageAccount.Parse connectionString
    let tableClient = account.CreateCloudTableClient()    
    tableClient.GetTableReference(gamesTable).CreateIfNotExists() |> ignore
    tableClient.GetTableReference(gameNightsTable).CreateIfNotExists() |> ignore
    
    
    { new ITables with
        member _.InGameNightTable(operation) = inTableAsync tableClient gameNightsTable operation
        member _.FromGameNightTable(query) = fromTableAsync tableClient gameNightsTable query 
        member _.InGameTable(operation) = inTableAsync tableClient gamesTable operation
        member _.FromGameTable(query) = fromTableAsync tableClient gamesTable query }