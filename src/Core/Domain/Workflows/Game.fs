module Domain.Workflows.Game

open Domain
open FSharp.UMX
open System
open FsToolkit.ErrorHandling

type AddGameRequest =
    { Id : Guid<GameId>
      GameName : string<CanonizedGameName>
      CreatedBy : string<CanonizedUsername>
      ImageUrl : string option
      Link : string option
      Notes : string option
      NumberOfPlayers : string option
      ExistingGames : Set<string<CanonizedGameName>> }

type AddGame = AddGameRequest -> Result<Game, string>

let addGame : AddGame =
    fun req ->
        if Set.contains req.GameName req.ExistingGames then
            Error "Duplicate game"
        else 
            { Game.Id = req.Id
              CreatedBy = req.CreatedBy
              ImageUrl = req.ImageUrl
              Link = req.Link 
              Name = req.GameName 
              Notes = req.Notes
              NumberOfPlayers = req.NumberOfPlayers }
            |> Ok

type UpdateGameRequest =
    { Id: Guid<GameId>
      CurrentGameName: string<CanonizedGameName>
      GameName : string<CanonizedGameName>
      CreatedBy : string<CanonizedUsername>
      ImageUrl : string option
      Link : string option
      Notes : string option
      NumberOfPlayers : string option
      ExistingGames : Set<string<CanonizedGameName>> }

type UpdateGame = UpdateGameRequest -> Result<Game, string>
let updateGame : UpdateGame =
    fun req ->
        if Set.contains req.GameName req.ExistingGames then
            Error "Duplicate game"
        else 
            { Id = req.Id
              CreatedBy = req.CreatedBy
              ImageUrl = req.ImageUrl
              Link = req.Link 
              Name = req.GameName 
              Notes = req.Notes
              NumberOfPlayers = req.NumberOfPlayers }
            |> Ok