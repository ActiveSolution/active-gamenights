module Backend.Domain.ProposedGameNight

open System
open Backend
open FsToolkit.ErrorHandling


type CreateProposedGameNightRequest =
    { Games : Set<GameName>
      Dates : Set<FutureDate>
      CreatedBy : User }
type GameVoteRequest =
    { GameNight : ProposedGameNight
      GameName : GameName 
      User : User }
type DateVoteRequest =
    { GameNight : ProposedGameNight
      Date : Date 
      User : User }
      
type CreateProposedGameNight = CreateProposedGameNightRequest -> ProposedGameNight
type AddGameVote = GameVoteRequest -> ProposedGameNight
type AddDateVote = DateVoteRequest -> ProposedGameNight
type RemoveGameVote = GameVoteRequest -> ProposedGameNight
type RemoveDateVote = DateVoteRequest -> ProposedGameNight

module CreateProposedGameNightRequest =
    let create(games, dates, createdBy) =
        if games |> List.length < 1 then
            ValidationError "Must provide at least one game" |> Error
        elif dates |> List.length < 1 then
            ValidationError "Must provide at least one date" |> Error
        else
            result {
                let! gameNames =
                    games
                   |> List.choose (fun s -> if String.IsNullOrWhiteSpace(s) then None else Some s)
                   |> List.map GameName.create
                   |> List.sequenceResultM
                let! dates =
                    dates
                    |> List.choose (fun s -> if String.IsNullOrWhiteSpace s then None else Some s)
                    |> List.map FutureDate.tryParse
                    |> List.sequenceResultM
                    
                return
                    { Games = Set.ofList gameNames
                      Dates = Set.ofList dates
                      CreatedBy = createdBy } 
            }
        
module GameVoteRequest =
    let create(gameNight, gameName, user) =
        { GameNight = gameNight
          GameName = gameName
          User = user }
          
module DateVoteRequest =
    let create(gameNight, date, user) =
        { GameNight = gameNight
          Date = date
          User = user }

let createProposedGameNight : CreateProposedGameNight =
    fun req ->
        let games =
            req.Games
            |> Set.map (fun g -> g, Set.empty)
            |> Map.ofSeq
        let dates =
            req.Dates
            |> Seq.map (fun (FutureDate date) -> date, Set.empty)
            |> Map.ofSeq
            
        { ProposedGameNight.Id = GameNightId.newId()
          GameVotes = games
          DateVotes = dates
          CreatedBy = req.CreatedBy }

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
