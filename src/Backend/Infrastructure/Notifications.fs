module Infrastructure.Notifications
open Backend
open Domain
open Lib.AspNetCore.ServerSentEvents
open Microsoft.AspNetCore.Http
open Giraffe


let exceptClient (currentUser: User) =
    System.Func<_,_>(fun (client: IServerSentEventsClient) ->
        match client.User |> ClaimsPrincipal.getUser with
        | Ok clientUser when clientUser.Id = currentUser.Id -> false
        | _ -> true)

let sendEvent (ctx: HttpContext) clientPredicate text =
    let service = ctx.GetService<IServerSentEventsService>()
    service.SendEventAsync(text = text, clientPredicate = clientPredicate) |> Async.AwaitTask
