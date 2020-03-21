namespace test

open Newtonsoft.Json

module Say =
    let hello name =
        let o = {| Name = name; Greeting = "Hello" |}
        sprintf "%s" (JsonConvert.SerializeObject o)
