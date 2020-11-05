module Backend.CompositionRoot

open Backend

let config = Configuration.config.Value

let storage = Storage.Service config.ConnectionString

