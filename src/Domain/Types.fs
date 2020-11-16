namespace Domain

open System
open FSharpPlus.Data

[<AutoOpen>]
module Types =

    type ValidationError = ValidationError of string
    type DomainError = DomainError of string
    type NotFoundError = NotFoundError

    type GameName = GameName of string
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
          Link : Uri option
          ImageUrl : string option
          Notes : string option }
    type Date =
        { Year : int
          Month : int
          Day : int }
    type FutureDate = FutureDate of Date
    type GameNightId = GameNightId of Guid
    type ProposedGameNight =
        { Id : GameNightId
          GameVotes : NonEmptyMap<GameName, Set<User>>
          DateVotes : NonEmptyMap<Date, Set<User>>
          CreatedBy : User }
    type ConfirmedGameNight =
        { Id : GameNightId 
          GameVotes : NonEmptyMap<GameName, Set<User>>
          Date : Date
          Players : NonEmptySet<User>
          CreatedBy : User }
    type CancelledGameNight =
        { Id : GameNightId
          GameVotes : NonEmptyMap<GameName, Set<User>>
          DateVotes : NonEmptyMap<Date, Set<User>>
          CreatedBy : User }
          
