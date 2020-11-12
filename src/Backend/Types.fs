[<AutoOpen>]
module Backend.Types

open System
open System.Threading.Tasks

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
      CreatedBy : User }
type ConfirmedGameNight =
    { Id : GameNightId 
      GameVotes : GameVotes
      Date : Date
      Players : Set<User>
      CreatedBy : User }
type CancelledGameNight =
    { Id : GameNightId
      GameVotes : GameVotes
      DateVotes : DateVotes
      CreatedBy : User }


type AsyncResult<'TResult, 'TError> = Async<Result<'TResult, 'TError>>

type ValidationError = ValidationError of string
type DomainError = DomainError of string
type NotFoundError = NotFoundError
type BrowserError =
    | Duplicate 
    | NotFound of NotFoundError
    | Validation of ValidationError
    | Domain of DomainError
    | MissingUser of string
    
type ApiError =
    | Duplicate 
    | NotFound of NotFoundError
    | Validation of ValidationError
    | Domain of DomainError
    

// Web
type BasePath = BasePath of string
type Domain = Domain of string
type BrowserResponse =
    | Html of string
    | Redirect of string
type BrowserResult = Result<BrowserResponse, BrowserError>
type BrowserTaskResult = Task<BrowserResult>

type ApiResponse<'T> =
    | Json of 'T
    | Created of Location: string
    | Accepted
type ApiResult<'T> = Result<ApiResponse<'T>, ApiError>
type ApiTaskResult<'T> = Task<ApiResult<'T>>
