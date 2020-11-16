module Common.Json

open Newtonsoft.Json

let serialize value =
    JsonConvert.SerializeObject value
let deserialize<'T> value =
    JsonConvert.DeserializeObject<'T> value