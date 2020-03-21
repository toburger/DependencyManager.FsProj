module test2

open Suave
open Suave.Filters
open Suave.Operators

let app jsonPayload =
    choose [
        GET >=> Writers.addHeader "Content-Type" "application/json" >=> Successful.OK jsonPayload
        POST >=> RequestErrors.BAD_REQUEST "You cannot post here!"
    ]
