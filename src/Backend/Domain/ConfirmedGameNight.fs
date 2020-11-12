module Backend.Domain.ConfirmedGameNight

open Backend
open FsToolkit.ErrorHandling

type ConfirmResult =
    | Confirmed of ConfirmedGameNight
    | Cancelled of CancelledGameNight

type ConfirmGameNight = ProposedGameNight -> ConfirmResult

let confirmGameNight : ConfirmGameNight =
    fun proposed ->
        let winningDate, players =
            proposed.DateVotes
            |> Map.toList
            |> List.maxBy (fun (_, votes) -> votes.Count)
        
        if players.Count < 2 then
            Cancelled
                { CancelledGameNight.Id = proposed.Id
                  DateVotes = proposed.DateVotes
                  GameVotes = proposed.GameVotes
                  CreatedBy = proposed.CreatedBy }
        else
            Confirmed
                { ConfirmedGameNight.Id = proposed.Id
                  ConfirmedGameNight.Date = winningDate
                  ConfirmedGameNight.Players = players
                  ConfirmedGameNight.GameVotes = proposed.GameVotes
                  ConfirmedGameNight.CreatedBy = proposed.CreatedBy }
                