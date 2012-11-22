namespace Cantos

///Markdown conversion.
[<AutoOpen>]
module Markdown =

    open System.IO
    open MarkdownDeep
    open System

    ///Converts markdown to html.
    let transformMarkdown markdown = 
        let md = Markdown()
        md.ExtraMode <- false
        md.SafeMode <- false
        //TODO get a streaming convertor?
        md.Transform(markdown)

    let processExtensions = [ "md"; "markdown" ] |> List.map FileExtension.Create

    ///Turns a markdown stream into and html stream.  Returns a new Uri with .html extension.
    let markdownProcessor (streamInfo:StreamInfo) = 
        match streamInfo with

        | TextOutput(x) when x.Path.HasExtension(processExtensions) ->
            let path = x.Path.ChangeExtension(FileExtension.Create("html"))
            use tr = x.ReaderF()
            let html = transformMarkdown (tr.ReadToEnd())
            let f = fun () -> new StringReader(html) :> TextReader
            TextOutput({ Path = path; HadFrontMatter = x.HadFrontMatter; ReaderF = f; Meta = x.Meta })

        | TextOutput(_) | BinaryOutput(_) -> streamInfo
        



