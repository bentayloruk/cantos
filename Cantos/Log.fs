///The home of logging (and mutation!).
[<AutoOpen>]
module Cantos.Log 

open System

type LogLevels = | Info | Warning | Error | Success

///Write to the Console.
let consoleLogWriter logLevel (msg:string) = 
    let x = Console.ForegroundColor
    Console.ForegroundColor <- 
        match logLevel with
        | Info -> x 
        | Error -> ConsoleColor.Red
        | Warning -> ConsoleColor.DarkYellow
        | Success -> ConsoleColor.Green
    System.Console.WriteLine(msg)
    Console.ForegroundColor <- x

//Our only mutable!?
let mutable writeLine = consoleLogWriter

//Handy log functions...
let logError = writeLine Error
let logInfo = writeLine Info 
let logWarning = writeLine Warning 
let logSuccess = writeLine Success 
let logErrorException msg (ex:Exception) =
    //This is a bit crap.
    writeLine Error msg
    writeLine Error ex.Message

