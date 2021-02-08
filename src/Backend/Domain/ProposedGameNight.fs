namespace Domain

open FSharpPlus.Data

module ProposedGameNight =
    let gameNightsWhereUserHasNotVoted (allGameNights: ProposedGameNight list) user = 
        allGameNights 
        |> List.choose (fun g -> 
            let gameVoters = g.GameVotes |> NonEmptyMap.values |> Seq.collect Set.toSeq |> Set.ofSeq
            let dateVoters = g.DateVotes |> NonEmptyMap.values |> Seq.collect Set.toSeq |> Set.ofSeq
            let allVoters = gameVoters + dateVoters
            if Set.contains user allVoters then None else Some g.Id)
        |> List.length

