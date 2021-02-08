namespace Domain

open System
open FSharp.UMX
open FsToolkit.ErrorHandling

module User =

    let createUsername str =
        if String.IsNullOrWhiteSpace str then
            Error "User cannot be empty"
        else
            Helpers.canonize str
            |> UMX.tag<CanonizedUsername> 
            |> Ok
        
    let toDisplayName (name: string<CanonizedUsername>) =
        %name |> Helpers.unCanonize
        
    let create username =
        result {
            let! username = createUsername username
            let id = Guid.NewGuid() |> UMX.tag<UserId>
            return { User.Id = id; Name = username }
        }
    
    let parseUserId str =
        str
        |> Helpers.tryParseGuid
        |> Result.requireSome (sprintf "%s is not a valid guid" str)
        |> Result.map UMX.tag<UserId>

    let deserialize(str: string) =
        result {
            match str |> Option.ofObj with
            | Some str ->
                let arr = str.Split ","
                let! userId = parseUserId arr.[0]
                let! username = createUsername arr.[1]
                return
                    { Id = userId
                      Name = username }
            | None -> return! Error "Missing user"
        }
        
    let serialize (user: User) =
        sprintf "%s,%s" (user.Id.ToString()) %user.Name
