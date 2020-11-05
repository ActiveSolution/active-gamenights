module Backend.Domain

open System
open Backend.Extensions
open Backend.Helpers

let createUser : CreateUser = User.create 
let createGame : CreateGame = fun _ _ -> notImplemented()
let createProposedGameNight : CreateProposedGameNight =
    fun req ->
        let games =
            req.Games
            |> List.map (fun g -> g, Set.empty)
            |> Map.ofList
        let dates =
            req.Dates
            |> List.map (fun (FutureDate date) -> date, Set.empty)
            |> Map.ofList
        { ProposedGameNight.Id = GameNightId.newId()
          Games = games
          Dates = dates
          ProposedBy = req.ProposedBy }

let addGameVote : AddGameVote =
    fun req ->
        let newVotes = 
            req.GameNight.Games 
            |> Map.tryFind req.GameName 
            |> Option.defaultValue Set.empty 
            |> Set.add req.User
        let newGames =
            req.GameNight.Games |> Map.add req.GameName newVotes
        { req.GameNight with Games = newGames}
        
let removeGameVote : RemoveGameVote =
    fun req ->
        let newVotes = 
            req.GameNight.Games 
            |> Map.tryFind req.GameName 
            |> Option.defaultValue Set.empty 
            |> Set.remove req.User
        let newGames =
            req.GameNight.Games |> Map.add req.GameName newVotes
        { req.GameNight with Games = newGames}
let addDateVote : AddDateVote =
    fun req ->
        let newVotes = 
            req.GameNight.Dates
            |> Map.tryFind req.Date
            |> Option.defaultValue Set.empty 
            |> Set.add req.User
        let newDates =
            req.GameNight.Dates |> Map.add req.Date newVotes
        { req.GameNight with Dates = newDates }
        
let removeDateVote : RemoveDateVote =
    fun req ->
        let newVotes = 
            req.GameNight.Dates
            |> Map.tryFind req.Date
            |> Option.defaultValue Set.empty 
            |> Set.remove req.User
        let newDates =
            req.GameNight.Dates |> Map.add req.Date newVotes
        { req.GameNight with Dates = newDates }
