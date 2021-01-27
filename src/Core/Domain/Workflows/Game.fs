module Domain.Workflows.Game

open Domain
open FSharp.UMX
open System

type AddGameRequest =
    { GameName : string<CanonizedGameName>
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
            { Game.CreatedBy = req.CreatedBy
              Game.ImageUrl = req.ImageUrl
              Game.Link = req.Link 
              Game.Name = req.GameName 
              Game.Notes = req.Notes
              Game.NumberOfPlayers = req.NumberOfPlayers }
            |> Ok

type UpdateGameRequest =
    { GameName : string<CanonizedGameName>
      CreatedBy : string<CanonizedUsername>
      ImageUrl : string option
      Link : string option
      Notes : string option
      NumberOfPlayers : string option }

type UpdateGame = UpdateGameRequest -> Game
let updateGame : UpdateGame =
    fun req ->
        { Game.CreatedBy = req.CreatedBy
          Game.ImageUrl = req.ImageUrl
          Game.Link = req.Link 
          Game.Name = req.GameName 
          Game.Notes = req.Notes
          Game.NumberOfPlayers = req.NumberOfPlayers }