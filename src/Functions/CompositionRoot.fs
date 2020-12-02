module Functions.CompositionRoot

open Backend
open Common

let config = Configuration.config.Value

let storage = Storage.Service config.ConnectionString

