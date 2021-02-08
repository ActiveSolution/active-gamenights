namespace Domain

module Game =
    let toMap (games: seq<Game>) =
        games 
        |> (fun gs -> gs |> Seq.map (fun g -> g.Id, g) |> Map.ofSeq)