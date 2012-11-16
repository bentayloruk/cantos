module Markdown

open MarkdownDeep
open System.IO

let (|MarkdownFile|_|) path =
    match Path.GetExtension(path).ToLower() with
    | ".md" | ".markdown" -> Some(path) 
    | _ -> None 

///Converts markdown to html.
let mdToHtml markdown = 
    let md = Markdown()
    md.ExtraMode <- false
    md.SafeMode <- false
    md.Transform(markdown)


