[<AutoOpen>]
module Exception
open System

//Note: assume inlining will mean the stack trace is cleaner.  Is this what raise does?
let inline raiseNotImpl msg = raise <| NotImplementedException(msg)

let inline raiseArgEx msg (arg:string) = raise <| ArgumentException(msg, arg)

let inline raiseInvalidOp msg = raise <| InvalidOperationException(msg)
