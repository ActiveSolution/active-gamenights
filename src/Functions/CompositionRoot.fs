module Functions.CompositionRoot

open Backend

let config = Configuration.config.Value

type FunctionsEnv() =
    interface Storage.IStorage with member _.Tables = Storage.live config.ConnectionString
let env = FunctionsEnv()
