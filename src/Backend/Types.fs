[<AutoOpen>]
module Backend.Types

open System
open System.Threading.Tasks
open Backend.Turbolinks

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
      Games : GameVotes
      Dates : DateVotes
      ProposedBy : User }
type ConfirmedGameNight =
    { Id : GameNightId 
      Games : (GameName) list
      Date : Date
      Players : User list }

type BasePath = BasePath of string
type ApiResponse =
    | Html of string
    | Redirect of string
type ApiResult = Task<Result<ApiResponse, AppError>>
    
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

    
open FsToolkit.ErrorHandling
// Implementations
module private Common =
    let tryParseGuid (str: string) =
        match Guid.TryParse str with
        | true, g -> Some g
        | false, _ -> None
module User =
    let create str =
        let str = Helpers.canonize str
        if String.IsNullOrWhiteSpace str then
            Error (ValidationError "User cannot be empty")
        else User str |> Ok
        
module GameName =
    let create str =
        let str = Helpers.canonize str
        if String.IsNullOrWhiteSpace str then
            Error (ValidationError "GameName cannot be empty")
        else GameName str |> Ok
    let value (GameName v) = v

module GameNightId =
    let value (GameNightId id) = id
    let parse (str: string) =
        str
        |> Common.tryParseGuid
        |> Result.requireSome (ValidationError "Not a valid guid")
        |> Result.map GameNightId
    let newId () = Guid.NewGuid() |> GameNightId
    let toString id = (value id).ToString()
    
module GameVotes =
    let create games : GameVotes =
        games
        |> List.map (fun g -> g, Set.empty)
        |> Map.ofList
        
    
module DateVotes =
    let create dates : DateVotes =
        dates
        |> List.map (fun d -> d, Set.empty)
        |> Map.ofList
        
module Date =
    let fromDateTime (dt : DateTime) =
        { Year = dt.Year
          Month = dt.Month
          Day = dt.Day }
    let today() = DateTime.Today |> fromDateTime
    let tryParse (str: string) =
        match DateTime.TryParse(str) with
        | true, dt -> Ok (fromDateTime dt)
        | false, _ -> sprintf "%s is not a valid date" str |> ValidationError |> Error
    let toDateTime (date: Date) =
        DateTime(date.Year, date.Month, date.Day)
    let toString date = date.Year.ToString("0000") + "-" + date.Month.ToString("00") + "-" + date.Day.ToString("00")

module FutureDate =
    let tryParse (str: string) =
        match DateTime.TryParse str with
        | true, dt -> 
            if dt.Date < DateTime.Today then Error (ValidationError "Future date must be in the future")
            else dt |> Date.fromDateTime |> FutureDate |> Ok
        | false, _ -> Error (ValidationError "Not a date")

module CreateGameRequest =
      let withName name user =
          { Game.Name = name
            CreatedBy = user
            NumberOfPlayers = None
            Link = None
            Notes = None }
    
    
open Saturn
module ApiResult =
    let handle ctx (res : ApiResult) =
        res
        |> Task.bind
            (fun r ->
                match r with
                | Ok (Html template) -> Controller.html ctx template
                | Ok (Redirect uri) -> Turbolinks.redirect ctx uri
                | Error (AppError.Validation (ValidationError err)) -> Response.badRequest ctx err
                | Error (AppError.MissingUser _) -> Turbolinks.redirect ctx "/user/add"
                | Error (AppError.NotFound _) -> Response.notFound ctx ()
                | Error (AppError.Duplicate)  -> Response.internalError ctx ())
        
// Extensions

type GameName with
    member this.Val = GameName.value this

type GameNightId with
    member this.Val = GameNightId.value this

type User with    
    member this.Val = this |> fun (User u) -> u

type BasePath with
    member this.Val = this |> fun (BasePath bp) -> bp
