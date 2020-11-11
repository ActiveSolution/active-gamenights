[<AutoOpen>]
module Backend.Types

open System
open System.Threading.Tasks

type AsyncResult<'TResult, 'TError> = Async<Result<'TResult, 'TError>>

type ValidationError = ValidationError of string
type NotFoundError = NotFoundError
type AppError =
    | Duplicate 
    | NotFound of NotFoundError
    | Validation of ValidationError
    | MissingUser of string
    
type GameName = GameName of string
type Link = string
type NumberOfPlayers = NumberOfPlayers of int
[<CustomEquality; CustomComparison>]
type User = User of string
    with
        override x.Equals(yObj) = 
            let username (User u) = u 
            match yObj with
            | :? User as y ->
                let xName = username x
                let yName = username y
                xName.Equals(yName, StringComparison.InvariantCultureIgnoreCase)
            | _ -> false

        override x.GetHashCode() = hash (x)
        
        interface IComparable with
            member x.CompareTo yObj =
                let username (User u) = u
                
                match yObj with
                | :? User as y ->
                    let xName = username x
                    let yName = username y
                    compare (xName.ToLower()) (yName.ToLower())
                | _ -> invalidArg "yObj" "cannot compare value of different types"
                
type Game = 
    { Name : GameName
      CreatedBy : User
      NumberOfPlayers : NumberOfPlayers option
      Link : Link option
      ImageUrl : string option
      Notes : string option }
type Date =
    { Year : int
      Month : int
      Day : int }
type FutureDate = FutureDate of Date
type GameNightId = GameNightId of Guid
type GameVotes = Map<GameName, Set<User>>
type DateVotes = Map<Date, Set<User>>
type ProposedGameNight =
    { Id : GameNightId
      GameVotes : GameVotes
      DateVotes : DateVotes
      ProposedBy : User }
type ConfirmedGameNight =
    { Id : GameNightId 
      GameVotes : GameVotes
      Date : Date
      Players : User list }

type BasePath = BasePath of string
type Domain = Domain of string
type BrowserResponse =
    | Html of string
    | Redirect of string
type BrowserResult = Result<BrowserResponse, AppError>
type BrowserTaskResult = Task<BrowserResult>

type ApiResponse<'T> =
    | Json of 'T
    | Created of Location: string
    | Accepted
type ApiResult<'T> = Result<ApiResponse<'T>, AppError>
type ApiTaskResult<'T> = Task<ApiResult<'T>>
    
// Domain
// Requests
type CreateGameRequest =
    { Name : GameName
      CreatedBy : User
      NumberOfPlayers : NumberOfPlayers option
      Link : Link option
      Notes : string option } 
type CreateProposedGameNightRequest =
    { Games : GameName list
      Dates : FutureDate list
      ProposedBy : User }
type AddGameRequest =
    { GameNightId : GameNightId
      GameName : GameName
      User : User }
type AddDateRequest =
    { GameNightId : GameNightId 
      Date : Date }
type GameVoteRequest =
    { GameNight : ProposedGameNight
      GameName : GameName 
      User : User }
type DateVoteRequest =
    { GameNight : ProposedGameNight
      Date : Date 
      User : User }
      
// Commands
type CreateUser = string -> Result<User, ValidationError>
type CreateGame = CreateGameRequest -> Game list -> Result<Game, string>
type CreateProposedGameNight = CreateProposedGameNightRequest -> ProposedGameNight
type AddGame = AddGameRequest -> (ProposedGameNight * Game option)
type AddDate = AddDateRequest -> ProposedGameNight
type AddGameVote = GameVoteRequest -> ProposedGameNight
type AddDateVote = DateVoteRequest -> ProposedGameNight
type RemoveGameVote = GameVoteRequest -> ProposedGameNight
type RemoveDateVote = DateVoteRequest -> ProposedGameNight
type ConfirmGameNight = ProposedGameNight -> Result<ConfirmedGameNight, string>
type AddPlayer = ConfirmedGameNight * User -> ConfirmedGameNight
type RemovePlayer = ConfirmedGameNight * User -> ConfirmedGameNight

// Queries
type GetGames = unit -> Async<Game list>
type GetProposedGameNights = unit -> Async<ProposedGameNight seq>

