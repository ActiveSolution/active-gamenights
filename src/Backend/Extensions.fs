[<AutoOpen>]
module Backend.Extensions

open Backend.Implementations
open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling

type GameName with
    member this.Val = GameName.value this

type GameNightId with
    member this.Val = GameNightId.value this
    member this.ToString() = this.Val.ToString()

type User with    
    member this.Val = this |> fun (User u) -> u

type BasePath with
    member this.Val = this |> fun (BasePath bp) -> bp

module HttpContext = 
    let usernameKey = "username"
    
type HttpContext with
    member this.GetUser() =
        this.Session.GetString(HttpContext.usernameKey)
        |> Option.ofObj
        |> Result.requireSome "Missing user in HttpContext"
        |> Result.mapError ValidationError
        |> Result.bind User.create
        
    member this.SetUsername(name) =
        this.Session.SetString(HttpContext.usernameKey, name)
        
    member this.ClearUsername() =
        this.Session.Remove(HttpContext.usernameKey)

        
module Map =
    let keys map = map |> Map.toList |> List.map fst
    let values map = map |> Map.toList |> List.map snd
    
    let change key f map =
        Map.tryFind key map
        |> f
        |> function
        | Some v -> Map.add key v map
        | None -> Map.remove key map

    let tryFindWithDefault defaultValue key map =
        map
        |> Map.tryFind key
        |> Option.defaultValue defaultValue
        

module Async =
    let map f xAsync =
        async {
            let! x = xAsync
            return f x
        }

module Result =
    let toOption xResult = 
        match xResult with 
        | Error _ -> None 
        | Ok v -> Some v
    let orFail xResult =
        match xResult with
        | Error _ -> failwith "Result is Error"
        | Ok v -> v
