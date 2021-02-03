namespace Domain

module Game =
    let toMap (games: Set<Game>) =
        games 
        |> Set.toSeq 
        |> (fun gs -> gs |> Seq.map (fun g -> g.Id, g) |> Map.ofSeq)