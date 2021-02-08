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
        
//    let parseUserId str =
//        str
//        |> Helpers.tryParseGuid
//        |> Result.requireSome (sprintf "%s is not a valid guid" str)
//        |> Result.map UMX.tag<UserId>
//
//    let deserialize(str: string) =
//        result {
//            let arr = str.Split ","
//            let! userId = parseUserId arr.[0]
//            let! username = createUsername arr.[1]
//            return
//                { Id = userId
//                  Name = username }
//        }
//        
//    let serialize (user: User) =
//        sprintf "%s,%s" (user.Id.ToString()) %user.Name
