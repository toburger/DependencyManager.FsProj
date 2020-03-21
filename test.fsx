#r "fsproj: ./test/test.fsproj"
#r "fsproj: ./test2/test2.fsproj"

open Suave

let json = test.Say.hello "Chris"

startWebServer defaultConfig (test2.app json)