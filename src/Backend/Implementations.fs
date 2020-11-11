[<AutoOpen>]
module Backend.Implementations

open System
open FsToolkit.ErrorHandling
open Turbolinks

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
            ImageUrl = None
            Link = None
            Notes = None }
    
open Saturn
module BrowserResult =
    let handle ctx (res : BrowserResult) =
        match res with
        | Ok (Html template) -> Controller.html ctx template
        | Ok (Redirect uri) -> Turbolinks.redirect ctx uri
        | Error (AppError.Validation (ValidationError err)) -> Response.badRequest ctx err
        | Error (AppError.Domain (DomainError err)) -> Response.badRequest ctx err
        | Error (AppError.MissingUser _) -> Turbolinks.redirect ctx "/user/add"
        | Error (AppError.NotFound _) -> Response.notFound ctx ()
        | Error (AppError.Duplicate)  -> Response.internalError ctx ()
        

module BrowserTaskResult =
    let handle ctx (res: BrowserTaskResult) =
        Task.bind (BrowserResult.handle ctx) res

module ApiResult =
    let handle ctx (res : ApiResult<_>) =
        match res with
        | Ok (Created uri) -> Response.created ctx uri
        | Ok (Json result) -> Controller.json ctx result
        | Ok (Accepted) -> Response.accepted ctx ()
        | Error (AppError.Validation (ValidationError err)) -> Response.badRequest ctx err
        | Error (AppError.Domain (DomainError err)) -> Response.badRequest ctx err
        | Error (AppError.MissingUser _) -> Turbolinks.redirect ctx "/user/add"
        | Error (AppError.NotFound _) -> Response.notFound ctx ()
        | Error (AppError.Duplicate)  -> Response.internalError ctx ()
        

module ApiTaskResult =
    let handle ctx (res: ApiTaskResult<_>) =
        Task.bind (ApiResult.handle ctx) res
        
