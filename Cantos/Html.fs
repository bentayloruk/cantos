module Html
open System.IO

let (|HtmlFile|_|) path =
    match Path.GetExtension(path).ToLower() with
    | ".html" | ".htm" -> Some(path) 
    | _ -> None 

