namespace Domain

open System
open FSharpPlus.Data
open FSharp.UMX

[<AutoOpen>]
module Domain =

    [<Measure>] type CanonizedGameName
    [<Measure>] type CanonizedUsername
    [<Measure>] type FutureDate
    [<Measure>] type GameNightId
                    
    type Game = 
        { Name : string<CanonizedGameName>
          CreatedBy : string<CanonizedUsername>
          NumberOfPlayers : string option
          Link : string option
          ImageUrl : string option
          Notes : string option }
    type ProposedGameNight =
        { Id : Guid<GameNightId>
          GameVotes : NonEmptyMap<string<CanonizedGameName>, Set<string<CanonizedUsername>>>
          DateVotes : NonEmptyMap<DateTime, Set<string<CanonizedUsername>>>
          CreatedBy : string<CanonizedUsername> }
    type ConfirmedGameNight =
        { Id : Guid<GameNightId> 
          GameVotes : NonEmptyMap<string<CanonizedGameName>, Set<string<CanonizedUsername>>>
          Date : DateTime
          Players : NonEmptySet<string<CanonizedUsername>>
          CreatedBy : string<CanonizedUsername> }
    type CancelledGameNight =
        { Id : Guid<GameNightId>
          GameVotes : NonEmptyMap<string<CanonizedGameName>, Set<string<CanonizedUsername>>>
          DateVotes : NonEmptyMap<DateTime, Set<string<CanonizedUsername>>>
          CreatedBy : string<CanonizedUsername> }
          
