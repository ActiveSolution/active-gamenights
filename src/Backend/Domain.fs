module Backend.Domain

open Backend
open FsToolkit.ErrorHandling

let createUser : CreateUser = User.create 
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
          GameVotes = games
          DateVotes = dates
          ProposedBy = req.ProposedBy }

let addGameVote : AddGameVote =
    fun req ->
        let newVotes = 
            req.GameNight.GameVotes 
            |> Map.tryFind req.GameName 
            |> Option.defaultValue Set.empty 
            |> Set.add req.User
        let newGames =
            req.GameNight.GameVotes |> Map.add req.GameName newVotes
        { req.GameNight with GameVotes = newGames}
        
let removeGameVote : RemoveGameVote =
    fun req ->
        let newVotes = 
            req.GameNight.GameVotes 
            |> Map.tryFind req.GameName 
            |> Option.defaultValue Set.empty 
            |> Set.remove req.User
        let newGames =
            req.GameNight.GameVotes |> Map.add req.GameName newVotes
        { req.GameNight with GameVotes = newGames}
let addDateVote : AddDateVote =
    fun req ->
        let newVotes = 
            req.GameNight.DateVotes
            |> Map.tryFind req.Date
            |> Option.defaultValue Set.empty 
            |> Set.add req.User
        let newDates =
            req.GameNight.DateVotes |> Map.add req.Date newVotes
        { req.GameNight with DateVotes = newDates }
        
let removeDateVote : RemoveDateVote =
    fun req ->
        let newVotes = 
            req.GameNight.DateVotes
            |> Map.tryFind req.Date
            |> Option.defaultValue Set.empty 
            |> Set.remove req.User
        let newDates =
            req.GameNight.DateVotes |> Map.add req.Date newVotes
        { req.GameNight with DateVotes = newDates }
