#r "fsproj: ./test/test.fsproj"
#r "fsproj: ./test2/test2.fsproj"

let json = test.Say.hello "Chris"
printfn "%s" json

open Suave

startWebServer defaultConfig (test2.app json)