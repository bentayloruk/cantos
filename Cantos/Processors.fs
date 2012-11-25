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
        

module Template = 

    ///Template types.
    type TemplateName = string
    type Template = { Name:TemplateName}
    type TemplateList = list<Template>
    type TemplateMap = Map<TemplateName, Template>
    type TemplateProvider = unit -> TemplateList

    open System.IO
    open FrontMatter

    (*
    let dirTemplateProvider path filter =
        ()
        Dir.getFiles path
        |> Seq.filter filter 
        |> Seq.map (fun templatePath ->
            try
                let args = readFileFrontMatterArgs templatePath
                let parent = 
                    match args with
                    | Some(valMap) when valMap.Contains("template") ->
                    | None -> None
                let parent = fmArgs.getArgValueOpt "template" fmArgs
                let template = reader.ReadToEnd()
                let templateFile = Path.GetFileName(templatePath)
                let values = valueTuples fmArgs |> List.ofSeq
                Some((templateFile, {FileName=templateFile; Template=template; ParentFileName=parent; Vars = values}))
            with
            | :? IOException -> None 
        )
        |> Seq.choose (fun x -> x)
        |> Map.ofSeq
    *)


///Used to create one or more books in the site.
module Books =

    ///Removes leading numbers and -.  Example:  1010-myname -> myname.  
    let deNumberWang (name:string) =
        let wangIndex = name.IndexOf('-')
        if wangIndex = -1 then name else
            let maybeNumber = name.Substring(0, wangIndex)
            let (parsed, number) = System.Int32.TryParse(maybeNumber)
            if parsed = true then name.Substring(wangIndex+1) else name 

    ///Removes leading numbers from file and dir paths (e.g. /2222-dirname/1234-file.html -> /dirname/file.html).
    let deNumberWangPath (path:string) =
        //Example:
        //This -> "developer\0100-introduction\0075-enticify-connector-for-commerce-server.md"
        //Becomes this -> "developer\introduction\enticify-connector-for-commerce-server.md"
        path.Split(Path.dirSeparatorChars)
        |> Seq.map deNumberWang 
        |> Array.ofSeq
        |> Path.combine


    [<RequireQualifiedAccessAttribute>]
    module Toc =

        open System.IO
        open FrontMatter

        ///Represents a Table of Contents.
        type Toc = { Name:string; Chapters:list<Chapter>; Path:RootedPath; }
        and Chapter = { Headings:list<Heading> }
        and Heading = { Href:string; Title:string; EnableLink: bool; }

        ///Creates an AHead for from a file.
        let maybeHeadingFromRootedPath sitePath = 
            Some( { Heading.Href = "TBD"; Title = "TBD"; EnableLink = true; } )

        ///Creates a TOC for files in the given site path.
        let forPath (path:RootedPath) name = 

            let tocSectionDirs = Directory.GetDirectories(path.AbsolutePath)

            let chaptersWithAtLeastOneHeading = 
                [
                    for tocSectionDir in tocSectionDirs do

                        let headings = 
                            Dir.getFiles tocSectionDir
                            |> Seq.map (fun filePath -> path.RelativeRootedPath(filePath)) 
                            |> Seq.choose maybeHeadingFromRootedPath 
                            |> List.ofSeq

                        if headings.Length > 0 then
                            yield { Chapter.Headings = headings }
                ]

            { 
                Path = path;
                Toc.Name = name;
                Chapters = chaptersWithAtLeastOneHeading;
            }
